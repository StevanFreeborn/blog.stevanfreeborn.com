```json meta
{
  "title": "Event-Driven Integration: Connecting Onspring and ServiceNow for Seamless Workflows",
  "lead": "Learn how to build an event-driven integration between Onspring and ServiceNow to streamline workflows, boost efficiency, and automate processes.",
  "isPublished": true,
  "publishedAt": "2025-04-04",
  "openGraphImage": "posts/event-driven-integration-connecting-onspring-and-servicenow-for-seamless-workflows/og-image.png"
}
```

I recently had the opportunity to work on a project that involved integrating [Onspring](https://onspring.com/) with [ServiceNow](https://servicenow.com/). The idea was pretty basic. Use some sort of key in Onspring to sync data over from ServiceNow. Your standard ETL job for the most part, but what made this project particularly interesting is the fact that in version 32 Onspring introduced this idea of a REST API outcome. This essentially allows data in Onspring to make requests out to other systems when something happens to that data. What this means for ETL jobs like this is you don't necessarily have to rely on a scheduled task to facilitate the integration. Instead you can make the integration event-driven which gives you the benefits of having more up to date data since you don't have to wait for the scheduled task to run and you only process the data that needs to processed. Given how new this feature is, I thought it might be helpful to others to share how I went about building this integration using the REST API outcome.

> [!NOTE]
> Keep in mind that this content reflects my recent experience, the interfaces or exact steps might vary slightly as ServiceNow and Onspring update their platforms. When in doubt, refer to their official documentation for the most accurate information.

> [!NOTE]
> You can find all the code for this integration [here](https://github.com/StevanFreeborn/servicenow-poc).

## Technologies Used

Aside from the Onspring and ServiceNow platforms, I decided to write the actual integration in [TypeScript](https://www.typescriptlang.org/) and run it with [Node.js](https://nodejs.org/en/) primarily because I wanted to give [Hono](https://hono.dev/) a try. Hono is a lightweight web framework for building web apps and APIs that is compatible with a variety of runtimes and built around web standards. It also provides a much better developer experience when doing asynchronous programming than something like [Express](https://expressjs.com/).

For deploying and hosting the integration, I chose to dockerize the application and run it on my own VPS behind a reverse proxy. Obviously there are a lot of ways to this, but this made the most sense given the size and scope of the project. I use [GitHub](https://github.com) to host the project's repository and [Github Actions](https://github.com/features/actions) to build the Docker image and push it to [Docker Hub](https://hub.docker.com/) whenever I push to the main branch. This makes deploying as easy as SSHing into my VPS, pulling the latest image, and running it.

## Overview of the Integration

The integration is pretty simple. The idea is to allow Onspring to send a request to the integration whenever a particular event occurs in Onspring. This request will contain all the necessary information needed by the integration to get the desired data from ServiceNow and sync it back to Onspring. You can almost think of this as a webhook that Onspring can call whenever something happens that dictates that the data in Onspring needs to be updated. The integration will then take that request, get the data from ServiceNow, and send it back to Onspring. This is all done using the REST API outcome in Onspring and the public APIs that Onspring and ServiceNow provide.

Granted you might be thinking why go to the extra effort of doing this when you could just run a scheduled task to do the same thing. The answer is simple. This approach allows you to only process the data that needs to be processed and it allows you to have more up to date data since you don't have to wait for the scheduled task to run. This is particularly useful if you have a lot of data in Onspring and ServiceNow and you only need to process a small subset of that data. It also allows you to build more complex workflows that are triggered by events in Onspring or ServiceNow.

It also isn't to say that you can't blend the two approaches together. Maybe it makes sense to use a batch process initially to get the data into Onspring and then use the event-driven approach to keep it up to date. Or perhaps you want to have a scheduled task that runs on some interval, but you also want to be able to trigger the integration when needed based on events in Onspring. The point is really just to highlight the flexibility you now have with the REST API outcome.

## Building the Integration

### Identify the Data to Sync

The first step in any integration like this is figuring out what data you want to move between the systems involved. In this case, I wanted to sync data from ServiceNow to Onspring and specifically I wanted to sync data about applications. It is common for IT organizations to maintain an inventory of applications and their relevant details in ServiceNow. However, having this data available in Onspring as well can be useful for a variety of reasons across several different GRC functions. For example, you might want to use this data to help with risk assessments, audits, or compliance reporting.

In this case, I decided to sync the following fields from ServiceNow to Onspring:

- Application Name
- Application Owner
- Application Number
- Application Description
- Application Install Type
- Application Cloud Model
- Regulatory, Legal, and Compliance Type
- Application L3 Name
- Application Primary IT Owner

> [!NOTE]
> Some of these fields are custom fields.

I also needed to create the necessary app and fields in Onspring to store this data. This is pretty straightforward and can be done using the Onspring UI. You can create a new app and add the necessary fields to it. In this case, I created an app called "Business Applications" and added the following fields with the following types:

| Field Name                             | Field Type              |
| -------------------------------------- | ----------------------- |
| Application Name                       | Single-Line Text        |
| Application Owner                      | Single Select Reference |
| Application Number                     | Single-Line Text        |
| Application Description                | Multi-Line Text         |
| Application Install Type               | Single-Select List      |
| Application Cloud Model                | Single-Select List      |
| Regulatory, Legal, and Compliance Type | Reference               |
| Application L3 Name                    | Single-Select Reference |
| Application Primary IT Owner           | Single-Select Reference |

Quick note about relationships...both ServiceNow and Onspring have the concept of relationships and allow for difference pieces of data to be linked to one another. These relationships are represented in the API responses of each of their public APIs and can be managed using the APIs as well. For this integration I'm primarily dealing with relationships between applications and users. As you'll see in more detail later, I'll be making sure to capture the relationship between applications and users in ServiceNow and translating that to relationships in Onspring.

### Write the Integration

The integration itself is pretty simple. It is a simple web server that listens for requests from Onspring and then executes the necessary logic to get the data from ServiceNow and send it back to Onspring. I started by creating a new Hono app and defining a route that will handle the requests from Onspring. The route will be a POST request to the `/sync` endpoint. This is where Onspring will send the request when the REST API outcome is triggered. I also defined a route for the root endpoint that will return a simple JSON response. This is useful for testing purposes to make sure the server is running and responding to requests.

```typescript
import { serve } from "@hono/node-server";
import { Hono } from "hono";
import { HTTPException } from "hono/http-exception";

const app = new Hono();

app.onError((error, _) => {
  if (error instanceof HTTPException) {
    return error.getResponse();
  }

  return new Response("Internal Server Error", {
    status: 500,
    statusText: "An unhandled error has occurred",
  });
});

app.get("/", (c) => {
  return c.json({ message: "Hello!" });
});

app.post(
  "/sync",
  async (c) => {
    return c.json({ message: "Run sync" });
  },
);

const server = serve(
  {
    fetch: app.fetch,
    port: 3000,
  },
  (info) => {
    console.log(`Server is running on http://localhost:${info.port}`);
  },
);

for (const event of ["SIGINT", "SIGTERM"]) {
  process.on(event, () => {
    console.log(`Received ${event}, shutting down server`);
    server.close(() => {
      console.log("Server closed");
    });
  });
}
```

> [!NOTE]
> The event listeners at the end of the code are used to gracefully shut down the server when the process is terminated while running in a container. This is important to make sure that the server is properly closed and any resources are released.

In order for the sync to run properly, though, I'll need to ensure that the request Onspring sends to the integration contains all the necessary information. This I've done by taking advantage of the middleware functionality in Hono some of which it provides out of the box. The first middleware I added is for making sure the request provides the necessary authentication and authorization information for both the Onspring and ServiceNow APIs. I did this by extending the built in `BasicAuth` middleware with a custom `verifyUser` function that makes sure a username, password, and API key are provided.

```typescript
import { basicAuth } from "hono/basic-auth";
import { isNotNullUndefinedOrWhitespace } from "./utils.js";

app.post(
  "/sync",
  basicAuth({
    verifyUser: (username, password, c) => {
      const onspringApiKey = c.req.header("x-apikey");
      return (
        isNotNullUndefinedOrWhitespace(username) &&
        isNotNullUndefinedOrWhitespace(password) &&
        isNotNullUndefinedOrWhitespace(onspringApiKey)
      );
    },
  }),
  async (c) => {
    return c.json({ message: "Run sync" });
  },
);
```

Next I added a middleware to parse and validate the request body using the `zod` library. This is a great library for validating and parsing data and it works really well with Hono. I created a schema that defines the structure of the request body and then used the `zod` middleware to validate the request body against that schema.

```typescript
import { z } from "zod";
import { validator } from "hono/validator";

const syncRequestSchema = z.object({
  serviceNowBaseUrl: z.string().min(1).url(),
  appName: z.string().min(1),
  onspringUserAppId: z.number().min(1),
  onspringUserFirstNameFieldId: z.number().min(1),
  onspringUserLastNameFieldId: z.number().min(1),
  onspringUserUsernameFieldId: z.number().min(1),
  onspringUserEmailFieldId: z.number().min(1),
  onspringUserFullNameFieldId: z.number().min(1),
  onspringUserStatusFieldId: z.number().min(1),
  onspringUserStatusValue: z.string().min(1),
  onspringUserTierFieldId: z.number().min(1),
  onspringUserTierValue: z.string().min(1),
  onspringRegTypeAppId: z.number().min(1),
  onspringRegTypeIdFieldId: z.number().min(1),
});

app.post(
  "/sync",
  basicAuth({
    verifyUser: (username, password, c) => {
      const onspringApiKey = c.req.header("x-apikey");
      return (
        isNotNullUndefinedOrWhitespace(username) &&
        isNotNullUndefinedOrWhitespace(password) &&
        isNotNullUndefinedOrWhitespace(onspringApiKey)
      );
    },
  }),
  validator("json", (value, c) => {
    const parsed = syncRequestSchema.safeParse(value);

    if (parsed.success === false) {
      return c.json({ error: parsed.error }, 400);
    }

    return parsed.data;
  }),
  async (c) => {
    return c.json({ message: "Run sync" });
  },
);
```

> [!NOTE]
> You can think about the request body as a configuration file that Onspring will send to the integration. This configuration file contains all the necessary information needed by the integration to get the desired data from ServiceNow and sync it back to Onspring. This is useful because it allows you to change the configuration without having to change the code in the integration. You can just update the information in Onspring and the integration will use that info the next time it runs.

From here I implemented the actual sync logic. This is, for the most part, straightforward aside from the fact that as a part of the sync I'm also resolving the relationships between applications and users in ServiceNow and persisting those to Onspring. This involves checking if the user already exists in Onspring, adding them if they don't, and getting the record id of the user in Onspring so I can create the relationship between the application and the user by populating the proper reference fields in Onspring.

```typescript
import { serviceNow as sn } from "./serviceNow.js";
import { onspring as onx } from "./onspring.js";

app.post(
  "/sync",
  basicAuth({
    verifyUser: (username, password, c) => {
      const onspringApiKey = c.req.header("x-apikey");
      return (
        isNotNullUndefinedOrWhitespace(username) &&
        isNotNullUndefinedOrWhitespace(password) &&
        isNotNullUndefinedOrWhitespace(onspringApiKey)
      );
    },
  }),
  validator("json", (value, c) => {
    const parsed = syncRequestSchema.safeParse(value);

    if (parsed.success === false) {
      return c.json({ error: parsed.error }, 400);
    }

    return parsed.data;
  }),
  async (c) => {
    try {
      const onspringApiKey = c.req.header("x-apikey")!;
      const authHeaderValue = c.req.header("Authorization")!;
      const body = c.req.valid("json");
      const serviceNow = sn({
        baseUrl: body.serviceNowBaseUrl,
        auth: authHeaderValue,
      });
      const onspring = onx({ apiKey: onspringApiKey });

      const serviceNowApp = await serviceNow.getAppByName(body.appName);
      const [serviceNowAppOwner, serviceNowItOwner, serviceNowL3] = await Promise.all([
        serviceNow.getUserByLink(serviceNowApp.it_application_owner.link),
        serviceNow.getUserByLink(serviceNowApp.u_primary_it_owner.link),
        serviceNow.getUserByLink(serviceNowApp.u_l3_name.link),
      ]);

      let [appOwnerRecordId, itOwnerRecordId, l3RecordId, ...regulatoryRecordIds] =
        await Promise.all([
          onspring.getRecordIdByFieldValue({
            appId: body.onspringUserAppId,
            fieldId: body.onspringUserFullNameFieldId,
            value: serviceNowAppOwner.name,
          }),
          onspring.getRecordIdByFieldValue({
            appId: body.onspringUserAppId,
            fieldId: body.onspringUserFullNameFieldId,
            value: serviceNowItOwner.name,
          }),
          onspring.getRecordIdByFieldValue({
            appId: body.onspringUserAppId,
            fieldId: body.onspringUserFullNameFieldId,
            value: serviceNowL3.name,
          }),
          ...serviceNowApp.u_regulatory_legal_and_compliance
            .split(",")
            .map((reg) => {
              return onspring.getRecordIdByFieldValue({
                appId: body.onspringRegTypeAppId,
                fieldId: body.onspringRegTypeIdFieldId,
                value: reg,
              });
            }),
        ]);

      if (appOwnerRecordId === 0) {
        appOwnerRecordId = await onspring.saveRecord(
          newUserRecord({ userName: serviceNowAppOwner.name }),
        );
      }

      if (itOwnerRecordId === 0) {
        itOwnerRecordId = await onspring.saveRecord(
          newUserRecord({
            userName: serviceNowItOwner.name,
          }),
        );
      }

      if (l3RecordId === 0) {
        l3RecordId = await onspring.saveRecord(
          newUserRecord({ userName: serviceNowL3.name }),
        );
      }

      const response = {
        appName: serviceNowApp.name,
        shortName: serviceNowApp.number,
        description: serviceNowApp.short_description,
        installType: serviceNowApp.install_type,
        cloudModel: serviceNowApp.u_cloud_model,
        appOwner: appOwnerRecordId,
        itOwner: itOwnerRecordId,
        l3: l3RecordId,
        regulatory: regulatoryRecordIds.join("|"),
      };

      return c.json(response);

      function newUserRecord({ userName }: { userName: string }) {
        const [firstName, lastName] = userName.split(" ");
        const username = userName.replace(" ", ".").toLowerCase();

        return {
          appId: body.onspringUserAppId,
          fields: {
            [body.onspringUserFirstNameFieldId]: firstName,
            [body.onspringUserLastNameFieldId]: lastName,
            [body.onspringUserUsernameFieldId]: username,
            [body.onspringUserEmailFieldId]: `${username}@example.com`,
            [body.onspringUserStatusFieldId]: body.onspringUserStatusValue,
            [body.onspringUserTierFieldId]: body.onspringUserTierValue,
          },
        };
      }
    } catch (error) {
      if (error instanceof Error) {
        throw new HTTPException(500, {
          message: "Internal Server Error",
          res: new Response(JSON.stringify({ error: error.message }), {
            status: 500,
            statusText: "Internal Server Error",
            headers: {
              "Content-Type": "application/json",
            },
          }),
          cause: error,
        });
      }
    }
  },
);
```

Some of that code might not be super clear if you're not familiar with the Onspring API or the ServiceNow API, but the general idea is to get the data from ServiceNow and then send it back to Onspring. The `response` object is what will be sent back to Onspring and it contains all the necessary information needed to update the record in Onspring that triggered the REST API outcome.

### Deploy the Integration

Once the integration was built, I needed to deploy it. For my purposes I wrote a Dockerfile to containerize the application and then a workflow that builds the Docker image and pushes it to Docker Hub whenever I push to the main branch. The workflow also then deploys the image to my VPS by SSHing into the server, pulling the latest image, and running it.

Here is the Dockerfile:

```dockerfile
FROM node:20-alpine AS builder

WORKDIR /app

COPY package.json package-lock.json ./

RUN npm ci

COPY tsconfig.json .
COPY src/ ./src/

RUN npm run build

FROM node:20-alpine

WORKDIR /app

COPY package.json package-lock.json ./

RUN npm ci

COPY --from=builder /app/dist ./dist

EXPOSE 3000

CMD ["npm", "start"]
```

Here is the Github Actions workflow:

```yml
name: Deploy
on:
  workflow_dispatch:
  push:
    branches:
      - main
    paths-ignore:
      - '.github/**'
      - '.gitignore'
      - '**/*/LICENSE.md'
      - '**/*/README.md'
      - '**/*/Dockerfile'
jobs:
  build:
    name: Build and push Docker image
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.version.outputs.version }}
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - name: Login to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}
      - name: Create version tag
        id: version
        run: echo "version=$(date +%Y.%m.%d.%H%M%S)" >> $GITHUB_OUTPUT
      - name: Build and push image
        run: |
          TAG=${{ secrets.DOCKERHUB_USERNAME }}/servicenow-poc.stevanfreeborn.com:${{ steps.version.outputs.version }}
          docker build -t $TAG .
          docker push $TAG
  deploy:
    name: Deploy to server
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Run image on server
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.SSH_HOST }}
          username: ${{ secrets.SSH_USERNAME }}
          key: ${{ secrets.SSH_KEY }}
          script: |
            docker stop servicenow-poc.stevanfreeborn.com
            docker rm servicenow-poc.stevanfreeborn.com
            docker pull ${{ secrets.DOCKERHUB_USERNAME }}/servicenow-poc.stevanfreeborn.com:${{ needs.build.outputs.version }}
            docker run --restart always -d -p 6666:3000 --name servicenow-poc.stevanfreeborn.com ${{ secrets.DOCKERHUB_USERNAME }}/servicenow-poc.stevanfreeborn.com:${{ needs.build.outputs.version }}
```

There is some configuration involved to get traffic properly routed to the server and the container running on it, but I'm going to leave that outside the scope of this post. The important part is that the integration is running and can receive requests from Onspring.

### Create the REST API Outcome in Onspring

With the integration deployed, the next step is to create the trigger in Onspring that will execute the REST API outcome and kickoff the integration whenever the event occurs that I care about. In this case, I'm just going to send a request whenever a list field's value changes to "Yes" on an application record in Onspring. Here is an example of those configurations:

![example-trigger-logic](posts/event-driven-integration-connecting-onspring-and-servicenow-for-seamless-workflows/example-trigger-logic.png)

![example-rest-api-settings](posts/event-driven-integration-connecting-onspring-and-servicenow-for-seamless-workflows/example-rest-api-settings.png)

![example-request-settings](posts/event-driven-integration-connecting-onspring-and-servicenow-for-seamless-workflows/example-request-settings.png)

### Test the Integration

The final step is to test the integration. You can do that by creating a new application record in Onspring and setting the list field to "Yes". This should trigger the REST API outcome and send a request to the integration. The integration will then get the data from ServiceNow and send it back to Onspring. There is also a `Test Request` button in the REST API outcome settings that you can use to test the integration against an existing application record.

## Conclusion

Overall, this was a fun project to work on and it was a great opportunity to learn more about the ServiceNow APIs. The REST API outcome in Onspring is a powerful feature that allows you to build event-driven integrations that can help you automate processes and streamline workflows. I hope this post has been helpful in showing you how to build an integration between Onspring and ServiceNow using the REST API outcome.
