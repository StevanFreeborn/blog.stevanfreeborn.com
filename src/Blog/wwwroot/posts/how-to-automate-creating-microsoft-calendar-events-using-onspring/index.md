```json meta
{
  "title": "How to Automate Creating Microsoft Calendar Events Using Onspring",
  "lead": "Learn how to integrate Onspring with Microsoft Graph API to automate calendar event creation. Step-by-step guide with setup for Microsoft and Onspring and video examples included.",
  "isPublished": true,
  "publishedAt": "2025-01-04",
  "openGraphImage": "posts/how-to-automate-creating-microsoft-calendar-events-using-onspring/og-image.png",
}
```

I don't know if you heard, but Onspring recently [released a new version of the platform](https://software.onspring.com/hubfs/Release%20Notes/32.0%20Onspring%20Platform%20Release%20Notes.pdf) that includes a new REST API outcome. This new feature allows you to connect Onspring with other systems and automate tasks that previously would have required either waiting for Onspring to build a specific integration or creating your own with all the complexities that come with developing, deploying, hosting, and maintaining it.

One particular use case that this new feature could help solve that I've heard admins talk about over the years is the ability to automate certain calendar management tasks. In this post, I'll walk you through how you can use the new REST API outcome in Onspring to automate creating Microsoft calendar events.

> [!NOTE]
> Keep in mind that this content reflects my recent experience with the setup, the interfaces or exact steps might vary slightly as Microsoft and Onspring update their platforms. When in doubt, refer to their official documentation for the most accurate information.

## Let's Get Started - What You'll Need

Before we dive in, make sure you have:

- Access to Azure Portal
- Access to an Onspring instance
- A Microsoft 365 account

### Microsoft Setup

#### Setting Up in Azure - First Things First

The first major step is getting everything set up in Azure. At a high level, you'll need to register an application, provide the app with the right permissions, and configure credentials for the app to use when making requests against the Microsoft Graph API.

Head over to Azure and let's register your application:

- In the Azure Portal navigate to `App registrations`
- Click the `New registration` button
- Give your app a name
- Choose `Accounts in this organizational directory only` for the account type
- Don't worry about the redirect URI you can leave it blank
- Click the `Register` button

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-automate-creating-microsoft-calendar-events-using-onspring/register-an-app.mp4" controls title="Register An App"></video>

#### Permissions - Giving Your App the Access It Needs

Now your app needs the right permissions in order to make requests against the Microsoft Graph API on behalf of users to create calendar events. Here's how you can set that up:

- In your new app registration navigate to `API permissions`
- Click on the `Add a permission` button
- In the `Request API permissions` dialog select `Microsoft Graph`
- On the next screen, select `Application permissions` since you'll be asking Onspring to create events on behalf of users without any user interaction
- Search for `Calendar`, expand the `Calendar` group, and check the `Calendars.ReadWrite` permission checkbox
- Click the `Add permissions` button
- Finally, make sure to Grant admin consent for your tenant

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-automate-creating-microsoft-calendar-events-using-onspring/add-permissions-for-app.mp4" controls title="Add Permissions for App"></video>

#### Authenticating Your App - Getting a Client Secret

Okay, now we've got a way to identify our app to the Microsoft Graph API along with the permissions it needs. The last piece of the puzzle is to create a client secret that the app can use to authenticate itself when making requests. Here's how you can do that:

- In your new app registration navigate to `Certificates & secrets`
- Click the `New client secret` button
- Give your secret a description and choose an expiration period
- Click the `Add` button

> [!NOTE]
> Make sure to copy the secret value as you won't be able to see it again. Also remember to set a reminder to update the secret before it expires.

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-automate-creating-microsoft-calendar-events-using-onspring/create-client-secret.mp4" controls title="Create Client Secret"></video>

#### Getting Your Ducks in a Row - Important Info to Collect

Before we jump into Onspring, let's gather all the pieces of information you'll need:

- Your `Application (client) Id` (find this on the Overview page)
- That `Client Secret` we just created
- Your `Azure Tenant Id` (find this on the Overview page)
- The `Id` or `Unique Principal Name` of the user you want to create events for (you can find this in the Azure Portal under Users)

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-automate-creating-microsoft-calendar-events-using-onspring/collect-important-info.mp4" controls title="Collect Important Info"></video>

### The Fun Part - Setting Up Onspring

That takes care of the Microsoft side of things. We can move on to getting things squared away in Onspring. Keep in mind, all we are doing in Onspring is configuring it to make an API request to the Microsoft Graph API to create a calendar event when a trigger's logic is satisfied. If you want to test this out outside of Onspring, you can use a tool like Postman to do the same thing.

> [!NOTE]
> If you get curious about how exactly I know what information to provide in Onspring, you can find all the details in the Microsoft Graph API documentation [here](https://learn.microsoft.com/en-us/graph/auth/?context=graph%2Fapi%2F1.0&view=graph-rest-1.0) and [here](https://learn.microsoft.com/en-us/graph/api/user-post-events?view=graph-rest-1.0&tabs=http).

#### Create an App in Onspring

Let's start by creating an app in Onspring. We will use this app to essentially represent calendar events that we'd like to be created in a user's Microsoft calendar.

- In Onspring, navigate to the Admin area
- Click on the `Create` button and select `App`
- Stick with `Create a new App` and click the `Continue` button
- Give your app a name and click the `Save` button
- Add the following fields to your app and place them on the app's default layout:
  - `Subject` (Text)
  - `Start Date` (Date and Time)
  - `End Date` (Date and Time)
  - `Microsoft Event Id` (Text)

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-automate-creating-microsoft-calendar-events-using-onspring/create-onspring-app.mp4" controls title="Create Onspring App"></video>

#### Create the Trigger and REST API Outcome

Now that we have an app we can add events to, let's create a trigger that will kick off a request to the Microsoft Graph API to create a new calendar event whenever a new event is added to our app.

- In your app, navigate to the `Triggers` tab
- Click the `Add Trigger` button
- Give your trigger a name and click the `Save` button
- Change the status of the trigger to `Active`
- Navigate to the `Rules` tab and check the `Add "When Record is New" option to rule set` checkbox
- Navigate to the `Outcomes` tab and click on the `REST API` outcome
- Change the status of the outcome to `Active`
- Navigate to the `REST API Settings` tab and configure the options as follows:

```text
HTTP Method: POST
REST URL: https://graph.microsoft.com/v1.0/users/{INSERT USER IDENTIFIER HERE}/calendar/events
Authorization Type: OAuth 2.0
Access Token URL: https://login.microsoftonline.com/{INSERT TENANT ID HERE}/oauth2/v2.0/token
Client ID: Your Application (client) ID
Client Secret: The client secret you created
Scope: https://graph.microsoft.com/.default
```

- Navigate to the `Notifications` tab and add yourself in the `Notification Users` field
- Navigate to the `Request` tab
- Check the `Enable request body` checkbox
- Use the following JSON as your request body:

```json
{
  "subject": "{:Subject}",
  "start": {
    "dateTime": "{:Start Date}",
    "timeZone": "UTC"
  },
  "end": {
    "dateTime": "{:End Date}",
    "timeZone": "UTC"
  }
}
```

- In the `Data Mapping` grid add a field mapping for the `Microsoft Event Id` field to the `id` field in the response body
- Click the `Ok` button
- Click the `Save` button to save the trigger and outcome with all the configurations you've made

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-automate-creating-microsoft-calendar-events-using-onspring/create-trigger.mp4" controls title="Create Trigger"></video>

#### Create Your First Event

You should now have everything set up to start pushing events from Onspring to your Microsoft calendar. To test this out, add a new event to your app and check your calendar to see if the event shows up. You should also find that the id for the event in your calendar is stored in the Microsoft Event Id field in Onspring.

<video controls src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-automate-creating-microsoft-calendar-events-using-onspring/add-event.mp4" title="Add Event"></video>

## Conclusion

And there you have it! You've just set up automated calendar creation from Onspring. Pretty cool, right?

In the past, this would have required you to create a custom integration that polled Onspring for new events, made requests to the Microsoft Graph API to create the events, and then update Onspring with the results. You also would have had to work out the details around deploying, hosting, and maintaining that integration. With the new REST API outcome in Onspring, you can now automate this process without all the overhead that comes with building and maintaining a custom integration.

Obviously too, this is just one example of what you can do with the new REST API outcome in Onspring. The possibilities of how you can leverage it are really only limited by your access to and knowledge of working with APIs, your creativity, and your ability to think through the logic of how you want to automate tasks in Onspring.

## Want to Learn More?

Check out these resources for more details:

- [Microsoft Graph API Documentation](https://docs.microsoft.com/en-us/graph)
- [Onspring Version 32 Release Notes](https://software.onspring.com/hubfs/Release%20Notes/32.0%20Onspring%20Platform%20Release%20Notes.pdf)
