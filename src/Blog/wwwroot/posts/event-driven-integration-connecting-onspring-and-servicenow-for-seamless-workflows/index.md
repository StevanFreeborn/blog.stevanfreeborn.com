```json meta
{
  "title": "Event-Driven Integration: Connecting Onspring and ServiceNow for Seamless Workflows",
  "lead"": "Learn how to build an event-driven integration between Onspring and ServiceNow to streamline workflows, boost efficiency, and automate processes.",
  "isPublished": true,
  "publishedAt": "2025-03-25",
  "openGraphImage": "posts/event-driven-integration-connecting-onspring-and-servicenow-for-seamless-workflows/og-image.png",
}
```

I recently had the opportunity to work on a project that involved integrating [Onspring](https://onspring.com/) with [ServiceNow](https://servicenow.com/). The idea was pretty basic. Use some sort of key in Onspring to sync data over from ServiceNow. Your standard ETL job for the most part, but what made this project particularly interesting is the fact that in version 32 Onspring introduced this idea of a REST API outcome. This essentially allows data in Onspring to make requests out to other systems when something happens to that data. What this means for ETL jobs like this is you don't necessarily have to rely on a scheduled task to facilitate the integration. Instead you can make the integration event-driven which gives you the benefits of having more up to date data since you don't have to wait for the scheduled task to run and you only process the data that needs to processed. Given how new this feature is, I thought it might be helpful to others to share how I went about building this integration using the REST API outcome.

> [!NOTE]
> Keep in mind that this content reflects my recent experience, the interfaces or exact steps might vary slightly as ServiceNow and Onspring update their platforms. When in doubt, refer to their official documentation for the most accurate information.

> [!NOTE]
> You can find all the code for this integration [here](https://github.com/StevanFreeborn/servicenow-poc).

## Technologies Used

Aside from the Onspring and ServiceNow platforms, I decided to write the actual integration in [TypeScript](https://www.typescriptlang.org/) and run it with [Node.js](https://nodejs.org/en/) primarily because I wanted to give [Hono](https://hono.dev/) a try. Hono is a lightweight web framework for building web apps and APIs that is compatible with a variety of runtimes and built around web standards. It also provides a much better developer experience when doing asynchronous programming than something like [Express](https://expressjs.com/).

For deploying and hosting the integration, I chose to dockerize the application and run it on my own VPS behind a reverse proxy. Obviously there is a lot of ways to this, but this made the most sense given the size and scope of the project. I use [GitHub](https://github.com) to host the project's repository and [Github Actions](https://github.com/features/actions) to build the Docker image and push it to [Docker Hub](https://hub.docker.com/) whenever I push to the main branch. This makes deploying as easy as SSHing into my VPS, pulling the latest image, and running it.

## Overview of the Integration

The integration is pretty simple. The idea is to allow Onspring to send a request to the integration whenever a particular event occurs in Onspring. This request will contain all the necessary information needed by the integration to get the desired data from ServiceNow and sync it back to Onspring. You can almost think of this as a webhook that Onspring can call whenever something happens that dictates that the data in Onspring needs to be updated. The integration will then take that request, get the data from ServiceNow, and send it back to Onspring. This is all done using the REST API outcome in Onspring and the public APIs that Onspring and ServiceNow provide.

Granted you might be thinking why go to the extra effort of doing this when you could just run a scheduled task to do the same thing. The answer is simple. This approach allows you to only process the data that needs to be processed and it allows you to have more up to date data since you don't have to wait for the scheduled task to run. This is particularly useful if you have a lot of data in Onspring and ServiceNow and you only need to process a small subset of that data. It also allows you to build more complex workflows that are triggered by events in Onspring or ServiceNow.

It also isn't to say that you can't blend the two approaches together. Maybe it makes sense to use a batch process initially to get the data into Onspring and then use the event-driven approach to keep it up to date. Or perhaps you want to have a scheduled task that runs on some interval, but you also want to be able to trigger the integration when needed based on events in Onspring. The point is really just to highlight the flexibility you now have with the REST API outcome.

## Building the Integration

### Identify the Data to Sync

### Write the Integration

### Create the REST API Outcome in Onspring

### Test the Integration

## Conclusion


