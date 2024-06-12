```json meta
{
  "title": "Providing Real-Time Feedback About Long-Running Task with SignalR",
  "lead": "I've been developing OnxGraph to help Onspring admins visualize content relationships. To handle varying data loads without timeouts, I used SignalR for real-time updates. This post details setting up a task queue with Vue.js and ASP.NET Core to provide user feedback on long running background work.",
  "isPublished": true,
  "publishedAt": "2024-06-12",
  "openGraphImage": "posts/providing-real-time-feedback-about-long-running-task-with-signal-r/og-image.png",
}
```

Over the last few months I've been working on an app called [OnxGraph](https://onxgraph.stevanfreeborn.com) which is a tool for administrators of [Onspring](https://onspring.com) to visualize relationships between their content. When I began building it I knew that I was going to have to rely on talking to Onspring's public API to get the data I needed to display the graph's nodes and edges. However there is no way for me to know ahead of time how much data I would be dealing with. I could be dealing with a few nodes and edges which would only require a handful of API requests or many more that would require many API requests.

This presented the challenge of how to make sure that a user's request to create a graph didn't timeout while waiting for the data to be fetched as well as provide feedback to the user about the progress of the request. I decided this best approach was not to do all this work inline with the request but instead to queue the work and then provide the user with a way to check on the progress of the request once it was dequeued and processing.

The easiest solution here was probably just do some long polling. However I've been wanting to get some experience with [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr) for a while now and this seemed like a good opportunity to do so. SignalR is a library that makes it easy to add real-time web functionality to your applications. It's built on top of [WebSockets](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API) and abstracts away the complexity of managing connections. Plus as a fallback it will use polling if WebSockets aren't available.

I thought since I went through the process of setting this up for OnxGraph I'd write a short blog post about how to get it working. I'll be using a simple example of a task queue that processes tasks and sends updates to clients as the tasks are processed. I will have two parts to this example.

1. A simple [Vue.js](https://vuejs.org) client that will have a form to add a task to the queue and display the tasks added and update them after being processed.

2. A simple [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet) web api that will have a singleton service that manages an in-memory queue, a hosted background service that processes the queue and sends updates to the clients, an endpoint to add a task to the queue, and a hub that the clients can connect to to receive updates.

You can find all the code for this example in this [repo](https://github.com/StevanFreeborn/real-time-processing-with-signal-r).

## Setting up the client

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

Run client in dev mode so I can make changes and see them reflected in the browser right away.

```sh
npm run dev
```

## Setting up the server

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

Run server in watch mode for development because I will be making changes to it and hot reload is noice.

```sh
dotnet watch --project Server.API
```

## Setup debugging

Using a debugger is great and I think everyone should be using one. If you want to debug either the client or the server you most definitely can in this case. I've set this up in the example repo using visual studio code. Take a look at the `.vscode/launch.json` file.

## Implementation

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

Now let's start with the client and add a button that when clicked will add a task to the queue. I will also display the status of the task. I'll use the [Vue Composition API](https://v3.vuejs.org/guide/composition-api-introduction.html) to manage the state of the task.

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

I will now need to add that `add-task` endpoint to the server so that I can receive the task from the client and get it added into the queue.

Let's start by cleaning up the boiler plate code that comes with the web api template by updating `Program.cs` to remove all references to weather forecast.

Next I'll change the `weatherforecast` endpoint to `add-task` endpoint. And use the `MapPost` method instead of `MapGet` to add the endpoint. I'll start by just responding with a new task id.

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

At this point I should be able to go to the client and click the button to add a task to the queue. I should see the status change to `Adding task...` and then `Task added!`. If you see `Failed to add task` then something went wrong. You can check the console for more information.

### Implement the task queue

So that is cool. I can take a task from the client, send it to the server, and get a response back. But I still need to actually do something with that task so it can actually be processed. I'll start by creating a class to represent the task.

I'll use something really generic like a `BackgroundTask` class that has an `Id` property that is set to a new guid when the task is created.

```csharp
class BackgroundTask
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
}
```

Now that I can represent these tasks I need to create a queue to persist them while they are waiting to be processed. This in a real world scenario would likely be sorted by some sort of persistent store like RabbitMQ or Azure Service Bus. But for this example I'll just use an in-memory queue implemented with [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) and a class called `BackgroundTaskQueue` that will be registered as a singleton service.

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

This class has two methods `EnqueueAsync` and `DequeueAsync`. The `EnqueueAsync` method will add a task to the queue and the `DequeueAsync` method will remove a task from the queue. I can now consume these methods in the `TaskService` class that I'll create next.

### Implement the task service

I'm getting close to having everything wired up. But I am still missing a way to actual process these tasks which I am receiving from the client and sticking in my queue. For this I can create a class called `TaskService` that will be a hosted service that will run in the background and continuously pull tasks out of the queue and process them.

```csharp
class TaskService(BackgroundTaskQueue taskQueue) : BackgroundService
{
  private readonly BackgroundTaskQueue _taskQueue = taskQueue;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var task = await _taskQueue.DequeueAsync(stoppingToken);

      _ = Task.Run(async () =>
      {
        var startingUpdate = new { task.Id, Status = "Starting" };
        Console.WriteLine($"Task {task.Id} is starting");

        var randomNumberOfSeconds = new Random().Next(5, 30);
        await Task.Delay(TimeSpan.FromSeconds(randomNumberOfSeconds), stoppingToken);

        Console.WriteLine($"Task {task.Id} is completed in {randomNumberOfSeconds}");
      }, stoppingToken);
    }
  }
}
```

In the service of keeping things super simple in the example I am just going to log the start and stop of the task and simulate some async work with a random delay between 5 and 30 seconds. In a real world scenario I would be doing some actual work here.

> [!NOTE]
> I am wrapping the processing work in a call to `Task.Run` because in this scenario I am firing and forgetting the task and I don't want to block the background service from processing other tasks. In a real world scenario I would want to be more careful about how I handle exceptions when doing this.

### Update add task endpoint to add task to queue

Great I got my queue and I've got a service to process that queue, but my tasks aren't actually yet going into the queue even thought they are making it to the server. I'll update the `add-task` endpoint to actually add the task to the queue.

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

This gets to a full round trip of...

1. Task coming from the client
2. Task being received by the server
3. Task being added to the queue
4. Letting client know the task was added
5. Task being processed by the service

However I still haven't done anything to address the initial problem of providing feedback to the client about the progress of the task as it is being processed. I'll do that now.

### Brief overview of SignalR pieces

SignalR is going to allow me to have a real-time connection between the client and the server. I can then use this connection to send messages from the server to the client and vice versa. I can then have the client and server list for these messages and do something when they get them. This means I will need to setup something on either side of the connection to handle the sending and receiving of these messages.

In SignalR parlance this means that on the server I will have what SignalR calls a hub. This hub will allow me to establish connections with one or more clients when they want to connect. And on the client I will have a hub connection that will allow me to establish a connection with the server hub.

### Let's add the signal r hub

There isn't a lot to do to get this working. You'll need to first make sure you have the SignalR package installed.

```sh
dotnet add package Microsoft.AspNetCore.SignalR
```

Then you will need to register the SignalR service in `Program.cs` and map the hub to an endpoint of your choosing.

```csharp
builder.Services.AddSignalR();

app.MapHub<TaskHub>("/task-hub");
```

> [!NOTE]
> SignalR does support handling authentication and authorization. You can read more about that [here](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-8.0).

### Add client code to establish connection to server hub

On the client you'll need to also install the SignalR package.

```sh
npm install @microsoft/signalr
```

Then you will want to create a connection to the server hub so that you can listen to and send messages. The way you do this will depend on how exactly you are implementing your client, but the idea is generally the same. Since my client is a Vue.js app I will use the Vue Composition API to create a connection to the server hub.

When this component is served I will create a connection using the APIs provided by the `@microsoft/signalr` package and register a listener for the `ReceiveMessage` event that will just logged the message that is received. I will then start the connection when the component is mounted and also stop the connection when the component is unmounted.

```vue
<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { HubConnectionBuilder } from '@microsoft/signalr'

const updates = ref<string[]>([])

const connection = new HubConnectionBuilder()
  .withUrl('https://localhost:7138/task-hub', { withCredentials: false })
  .build()

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

Great I've got the ability establish a connection to the server hub when my client is served and listen for updates. I've also got the ability to add tasks to the queue and process them. However my client isn't actually getting any updates about the tasks as they are being processed. We'll have to go back to the server to fix that.

### Update server to actually send updates to client as tasks are processed

If you recall we add SignalR as a service in `Program.cs`. Doing this allows us to inject an instance of `IHubContext` into our services. This is what we will use to send messages to the clients. I'll update the `TaskService` to send a message to the clients when a task is started and when a task is completed. In this case the message is a simple object with the task id and the status of the task. SignalR will take care of serializing this object for me.

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

      _ = Task.Run(async () =>
      {
        var startingUpdate = new { task.Id, Status = "Starting" };
        Console.WriteLine($"Task {task.Id} is starting");
        await _taskHub.Clients.All.SendAsync("ReceiveMessage", startingUpdate, cancellationToken: stoppingToken);

        var randomNumberOfSeconds = new Random().Next(5, 30);
        await Task.Delay(TimeSpan.FromSeconds(randomNumberOfSeconds), stoppingToken);

        var finishedUpdate = new { task.Id, Status = $"Completed ({randomNumberOfSeconds} secs)" };
        Console.WriteLine($"Task {task.Id} is completed in {randomNumberOfSeconds}");
        await _taskHub.Clients.All.SendAsync("ReceiveMessage", finishedUpdate, cancellationToken: stoppingToken);
      }, stoppingToken);
    }
  }
}
```

I think it is good to point out that I am making sure that the string of text I am passing to the `SendAsync` method is the same as the string I am listening for on the client.

> [!NOTE]
> If you find this a bit brittle you are not alone. SignalR supports strongly typed hubs which you can read more about [here](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-8.0#strongly-typed-hubs).

Great now I should be able to display the updates on the client as the tasks are being processed.

### Update client to display updates instead of logging them

Again simple is the game here so all I am going to do is maintain an array of tasks in the client and update the status of the task as I receive updates from the server about it. I'll update the `ReceiveMessage` listener to update the status of the task in the array and then update the array so that the changes are reflected in the UI.

```vue
<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { HubConnectionBuilder } from '@microsoft/signalr'

type Task = {
  id: string
  status: string
}

const tasks = ref<Task[]>([])

const connection = new HubConnectionBuilder()
  .withUrl('https://localhost:7138/task-hub', { withCredentials: false })
  .build()

connection.on('ReceiveMessage', (taskUpdate: Task) => {
  const existingTask = tasks.value.find((update) => update.id === taskUpdate.id)

  if (existingTask === undefined) {
    tasks.value = [...tasks.value, taskUpdate]
    return
  }

  existingTask.status = taskUpdate.status

  tasks.value = [...tasks.value]
})
...
</script>

<template>
  <main>
    ...
    <div class="tasks-container">
      <h2>Tasks</h2>
      <ul>
        <li v-for="task in tasks" :key="task.id">{{ task.id }}: {{ task.status }}</li>
      </ul>
    </div>
  </main>
</template>

<style scoped>
...
.tasks-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
</style>
```

### Now you can run the client and server and see the updates as tasks are processed

![Tasks Processing](posts/providing-real-time-feedback-about-long-running-task-with-signal-r/tasks_processing.gif)

Pretty cool huh?

## Conclusion

I hope this post has been helpful in showing you how to get started with SignalR. I think it is a really powerful tool that can be used to add a lot of value to an application. I've only scratched the surface of what you can do with it here. I would encourage you to read the [documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-8.0) to learn more about what you can do with it.
