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

