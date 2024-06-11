```json meta
{
  "title": "Providing Real-Time Feedback About Long-Running Task with SignalR",
  "lead": "",
  "isPublished": false,
  "publishedAt": "<NEEDS SET>",
  "openGraphImage": "posts/providing-real-time-feedback-about-long-running-task-with-signal-r/og-image.png",
}
```

Over the last few months I've been working on an app called [OnxGraph](https://onxgraph.stevanfreeborn.com) which is a tool for administrators of [Onspring](https://onspring.com) to visualize relationships between their content. When I began building it I knew that I was going to have to rely on talking to Onspring's public API to get the data I needed to display the graph's nodes and edges. However there is no way for me to know ahead of time how much data I would be dealing with. I could be dealing with a few nodes and edges which would only require a handful of API requests or many more that would require many API requests.

This presented the challenge of how to make sure that a user's request to create a graph didn't timeout while waiting for the data to be fetched as well as provide feedback to the user about the progress of the request. I decided this best approach was not to do all this work inline with the request but instead to queue the work and then provide the user with a way to check on the progress of the request once it was dequeued and processing.

The easiest solution here was probably just do some long polling. However I've been wanting to get some experience with [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr) for a while now and this seemed like a good opportunity to do so. SignalR is a library that makes it easy to add real-time web functionality to your applications. It's built on top of [WebSockets](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API) and abstracts away the complexity of managing connections. Plus as a fallback it will use polling if WebSockets aren't available.

I thought since I went through the process of setting this up for OnxGraph I'd write a short blog post about how to get it working. I'll be using a simple example of a task queue that processes tasks and sends updates to clients as the tasks are processed. We will have two parts to this example.

1. A simple [Vue.js](https://vuejs.org) client that will have a form to add a task to the queue and display the tasks added and update them after being processed.

2. A simple [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet) web api that will have a singleton service that manages an in-memory queue, a hosted background service that processes the queue and sends updates to the clients, an endpoint to add a task to the queue, and a hub that the clients can connect to to receive updates.

You can find all the code for this example in this [repo](https://github.com/StevanFreeborn/onx-graph).

## Setting up client

```sh
npm create vue@latest
```

Answer the questions:

```sh
✔ Project name: … client
✔ Add TypeScript? … Yes
✔ Add JSX Support? … No
✔ Add Vue Router for Single Page Application development? … Yes
✔ Add Pinia for state management? … Yes
✔ Add Vitest for Unit testing? … Yes
✔ Add an End-to-End Testing Solution? … Playwright
✔ Add ESLint for code quality? … Yes
✔ Add Prettier for code formatting? … Yes
✔ Add Vue DevTools 7 extension for debugging? (experimental) … Yes
```

Install dependencies:

```sh
cd client
npm install
```

Setup https in development:

```sh
npm install --save-dev vite-plugin-mkcert
```

Edit vite config:

```ts
import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import VueDevTools from 'vite-plugin-vue-devtools'
import mkcert from'vite-plugin-mkcert'

// https://vitejs.dev/config/
export default defineConfig({
  server: {
    https: true,
  },
  plugins: [
    vue(),
    VueDevTools(),
    mkcert(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  }
})
```

Run client in dev mode so we can make changes and see them reflected in the browser right away.

```sh
npm run dev
```

## Setting up server

```sh
mkdir server
cd server
dotnet new webapi -o Server.API
dotnet new sln -n Server
dotnet sln add Server.API
```

Update `launchSettings.json` so it runs on https by default. You just have to make sure the `https` profile is the first one in the profiles object so it is the default profile.

```json
"profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "",
      "applicationUrl": "https://localhost:7138;http://localhost:5031",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
}
```

Update `Program.cs` so swagger ui launches on root. This is not necessary but makes it more convenient to access the swagger ui.

```csharp
app.UseSwaggerUI(config =>
{
    config.SwaggerEndpoint("/swagger/v1/swagger.json", "Server API");
    config.RoutePrefix = string.Empty;
});
```

Update `Program.cs` so client can make CORS requests. This is fine for development but you'll want to lock this down in production.

```csharp
...
builder.Services.AddCors(
  options => 
    options.AddDefaultPolicy(
      builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    )
);
...
app.UseCors();
```

Run server in watch mode for development because we will be making changes to it and hot reload is noice.

```sh
dotnet watch --project Server.API
```

## Setup debugging

Using a debugger is great and I think everyone should be using one. If you want to debug either the client or the server you most definitely can in this case. I've set this up in the example repo using visual studio code. Take a look at the `.vscode/launch.json` file.

### Allow client to add tasks to the queue

First some house keeping just to get things centered. Update `main.css` to contain the following styles:

```css
#app {
  max-width: 1280px;
  margin: 0 auto;
  padding: 2rem;
  font-weight: normal;
}
```

Now let's start with the client and add a button that when clicked will add a task to the queue. We will also display the status of the task. We will use the [Vue Composition API](https://v3.vuejs.org/guide/composition-api-introduction.html) to manage the state of the task.

```vue
<script setup lang="ts">
import { ref } from 'vue';


const addTaskStatus = ref<'idle' | 'pending' | 'success' | 'error'>('idle');

async function addTask() {
  try {
    addTaskStatus.value = 'pending';
    const result = await fetch('https://localhost:7138/add-task', { method: 'POST' });

    if (result.ok === false) {
      addTaskStatus.value = 'error';
      return;
    }

    addTaskStatus.value = 'success';
  } catch (error) {
    addTaskStatus.value = 'error';
  }
}

</script>

<template>
  <main>
    <div class="add-task-container">
      <button @click="addTask" type="button">Add Task</button>
      <Transition mode="out-in">
        <div v-if="addTaskStatus === 'pending'">Adding task...</div>
        <div v-else-if="addTaskStatus === 'success'">Task added!</div>
        <div v-else-if="addTaskStatus === 'error'">Failed to add task</div>
        <div v-else>Click the button to add a task</div>
      </Transition>
    </div>
  </main>
</template>

<style scoped>
main {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
}

.add-task-container {
  display: flex;
  align-items: center;
  gap: 1rem;
}
</style>
```

### Allow server to add tasks to the queue

We will now need to add that `add-task` endpoint to the server so that we can receive the task from the client and get it added into the queue.

Let's start by cleaning up the boiler plate code that comes with the web api template by updating `Program.cs` to remove all references to weather forecast.

Next we will change the `weatherforecast` endpoint to `add-task` endpoint. And use the `MapPost` method instead of `MapGet` to add the endpoint. We will start by just responding with a new task id.

```csharp
app
  .MapPost("/add-task", () =>
  {
    return new { Id = Guid.NewGuid().ToString() };
  })
  .WithName("AddTask")
  .WithDisplayName("Add Task")
  .WithDescription("Add a new task to the queue");
```

### Make sure the client can add tasks to the queue

At this point we should be able to go to the client and click the button to add a task to the queue. You should see the status change to `Adding task...` and then `Task added!`. If you see `Failed to add task` then something went wrong. You can check the console for more information.

### Implement the task queue

```csharp
class BackgroundTask
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
}
```

```csharp
class BackgroundTaskQueue
{
  private readonly Channel<BackgroundTask> _channel = Channel.CreateUnbounded<BackgroundTask>();

  public async Task EnqueueAsync(BackgroundTask task)
  {
    await _channel.Writer.WriteAsync(task);
  }

  public async Task<BackgroundTask> DequeueAsync(CancellationToken cancellationToken)
  {
    return await _channel.Reader.ReadAsync(cancellationToken);
  }
}
```

### Implement the task service

```csharp
class TaskService(BackgroundTaskQueue taskQueue) : BackgroundService
{
  private readonly BackgroundTaskQueue _taskQueue = taskQueue;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var task = await _taskQueue.DequeueAsync(stoppingToken);

      // Execute the task
      Console.WriteLine($"Task {task.Id} is starting");
  
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

      Console.WriteLine($"Task {task.Id} is complete");
    }
  }
}
```

### Update add task endpoint to add task to queue

```csharp
app
  .MapPost("/add-task", async (BackgroundTaskQueue queue) =>
  {
    var task = new BackgroundTask();
    await queue.EnqueueAsync(task);
    return Results.Json(data: task, statusCode: (int)HttpStatusCode.Created);
  })
  .WithName("AddTask")
  .WithDisplayName("Add Task")
  .WithDescription("Add a new task to the queue");
```

### Let's add signal r hub

```csharp
builder.Services.AddSignalR();

app.MapHub<TaskHub>("/task-hub");
```

### Add client code to establish connection to server hub

```sh
npm install @microsoft/signalr
```

```vue
<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { HubConnectionBuilder } from '@microsoft/signalr'

const updates = ref<string[]>([])

const connection = new HubConnectionBuilder().withUrl('https://localhost:7138/task-hub').build()

connection.on('ReceiveMessage', (message: string) => {
  console.log(message)
})

onMounted(() => {
  try {
    connection.start()
  } catch (error) {
    console.error(error)
  }
})

onUnmounted(() => {
  try {
    connection.stop()
  } catch (error) {
    console.error(error)
  }
})
</script>
...
```

### Update server CORS policy to allow signal r connections

```csharp
...
builder.Services.AddCors(
  options =>
    options.AddDefaultPolicy(
      builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .WithOrigins("https://localhost:5173")
    )
);
...
```

### Update server to actually send updates to client as tasks are processed

```csharp
class TaskService(
  BackgroundTaskQueue taskQueue,
  IHubContext<TaskHub> taskHub
) : BackgroundService
{
  private readonly BackgroundTaskQueue _taskQueue = taskQueue;
  private readonly IHubContext<TaskHub> _taskHub = taskHub;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var task = await _taskQueue.DequeueAsync(stoppingToken);

      // Execute the task
      await _taskHub.Clients.All.SendAsync("ReceiveMessage", $"Task {task.Id} is starting", cancellationToken: stoppingToken);

      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

      await _taskHub.Clients.All.SendAsync("ReceiveMessage", $"Task {task.Id} is complete", cancellationToken: stoppingToken);
    }
  }
}
```

### Update client to display updates instead of logging them

```vue
<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { HubConnectionBuilder } from '@microsoft/signalr'

const updates = ref<string[]>([])

const connection = new HubConnectionBuilder().withUrl('https://localhost:7138/task-hub').build()

connection.on('ReceiveMessage', (message: string) => {
  updates.value = [...updates.value, message]
})
...
</script>

<template>
  <main>
    ...
    <div class="updates-container">
      <h2>Updates</h2>
      <ul>
        <li v-for="(update, index) in updates" :key="index">{{ update }}</li>
      </ul>
    </div>
  </main>
</template>
...
```

### Now you can run the client and server and see the updates as tasks are processed

Pretty cool huh?

## Conclusion

Probably mentioned typed hubs here.
