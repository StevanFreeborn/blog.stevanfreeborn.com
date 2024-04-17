```json meta
{
  "title": "Providing Real-Time Feedback About Long-Running Task with SignalR",
  "lead": "",
  "isPublished": true,
  "publishedAt": "2024-04-15",
  "openGraphImage": "posts/providing-real-time-feedback-about-long-running-task-with-signal-r/og-image.png",
}
```

There are times when a user takes an action in your system that requires it to run in the background.

Things like:

- Generating a report
- Processing a large file
- Running a complex algorithm
- Sending a large number of emails

In these cases you don't want to hold your user hostage by making them wait for the task to complete before giving a response.

Instead you'd rather immediately acknowledge the user's request, add the task to some kind of queue, and then give the user feedback about how that task is progressing.

This leaves it up to the user if they want to wait for the task to complete or if they want to navigate away and come back later.

Traditionally you could provide this feedback with long polling, but websockets offer a way to open a persistent connection between the client and the server. The server then can push updates to the client in real-time.

SignalR is a library that makes it easy to add real-time web functionality to your applications. It's built on top of websockets and abstracts away the complexity of managing connections. Plus as a fallback it will use polling if websockets aren't available.

How to set this up?

- Let's build a simple client using vue
- Let's build a simple server using a .NET web api

The web api will have a singleton service that manages an in-memory queue.
The web api will have a hosted background service that processes the queue and sends updates to the clients.
The web api will have an endpoint to add a task to the queue.
The web api will have a hub that the clients can connect to to receive updates.
The client will have a form to add a task to the queue.
The client will then redirect to an edit page where it will connect to the hub and receive updates.
The task will have the following states:

- Pending
- Processing
- Completed
- Failed

The client will display the current state of the task and any messages that are sent with the updates.

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

Run client in dev

```sh
npm run dev
```

## Setting up server

```sh
mkdir server
cd server
dotnet new webapi -o Server.API
dotnet new xunit -o Server.Tests
dotnet add Server.API reference Server.Tests
dotnet new sln -n Server
dotnet sln add Server.API
dotnet sln add Server.Tests
```

Update launchSettings.json so it runs on https:

```json
"profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7138;http://localhost:5031",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
}
```

Update Program.cs so swagger ui launches on root:

```csharp
app.UseSwaggerUI(config =>
{
    config.SwaggerEndpoint("/swagger/v1/swagger.json", "Server API");
    config.RoutePrefix = string.Empty;
});
```

Update program.cs so client can make CORS requests:

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

Run server in watch mode for development:

```sh
dotnet watch --project Server.API
```

## Setup debugging

If you want to debug either the client or the server you most definitely can. I've set the my repo to do this using visual studio code.

## Add client code to add task to queue

Just some styling to make things centered.

In base.css add display flex to body

Replace app.css with:

```css
#app {
  max-width: 1280px;
  margin: 0 auto;
  padding: 2rem;
  font-weight: normal;
}
```

Replace App.vue with:

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
  justify-content: center;
  align-items: center;
}

.add-task-container {
  display: flex;
  align-items: center;
  gap: 1rem;
}
</style>
```

## Add server code to add task to queue

Update program.cs to remove all references to weather forecast.

Change weatherforecast endpoint to add-task endpoint. Map as post method That just returns new anonymous object with id property set to new guid.

```csharp
app
  .MapPost("/add-task", () =>
  {
    return new { Id = Guid.NewGuid().ToString() };
  })
  .WithName("AddTask")
  .WithDisplayName("Add Task")
  .WithDescription("Add a new task to the queue")
  .WithOpenApi();
```

Run client and server. Click the add task button. You should see a success message with id of the task just added.

### Implement the task queue

### Implement the task service

### Update add task endpoint to add task to queue

### Let's add signal r hub

### Add client code to establish connection to server hub

```sh
npm install @microsoft/signalr
```
