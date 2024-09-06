```json meta
{
  "title": "Leveraging Onspring's Flexibility and Extensibility: Automating Sanctions Checks",
  "lead": "Onspring is a flexible and extensible platform that can be used to automate a variety of business processes. In this post I'll show you how I used Onspring to build a solution that automates sanctions checks against the SDN list.",
  "isPublished": true,
  "publishedAt": "2024-09-06",
  "openGraphImage": "posts/automating-sanctions-checks-with-onspring/og-image.png",
}
```

## Onspring is flexible

I am admittedly [a bit biased](https://www.linkedin.com/in/stevan-freeborn/) in this opinion, but I think one of the best things about Onspring is its flexibility. Granted a lot of people probably view it through the lens of a GRC tool, but to me it is much more of a process automation platform that gives you a ton of functionality out of the box plus the ability to extend and augment that functionality to meet your specific needs. I've personally used it to build out a variety of applications for tracking my finances, managing my personal projects, and even tracking my workouts.

However recently I got the itch to build something a bit more in the vein of a traditional GRC use case after reading about sanctions. I knew about the concept of sanctions and the fact that they are used to punish countries or individuals for various reasons, but I hadn't ever really thought about the compliance aspect of it. Like who is responsible for making sure someone isn't doing business with a sanctioned entity? What is the process for checking if someone is sanctioned? How often should you check? What do you do if you find out someone is sanctioned?

I'm definitely not an expert nor a lawyer, but from what I've read it seems like at the very least if you hire someone or do business with someone you are supposed to check if they are sanctioned. Which made me think this is actually a pretty common problem that a lot of people probably need to solve for in some way and I'm guessing in a lot of cases it is just being done manually. So I thought this is a great example of a process that you could automate using Onspring.

## Automating the Process

Not to my surprise, there are actually quite a few existing services that aim at helping with this problem. Even the Office of Foreign Assets Control (OFAC) has a [search tool](https://sanctionssearch.ofac.treas.gov/) that you can use to check if someone is sanctioned. There are also a number of third party services that offer APIs that you can integrate with to build automated processes, but none of them are free. Not that they should be, but it just seemed like you could definitely recreate a version of that using the data that OFAC provides publicly for free, Onspring, and a little bit of glue code.

What I decided I wanted was basically a plugin like solution that anyone could tie into within an Onspring instance if they wanted to perform sanctions checks against the SDN list. They'd just connect their app or survey to this solution's app via a reference field and either create a record manually or automatically create a record when a new vendor, contact, employee, user, etc. is created. Could even then be done on a recurring basis too. In this way Onspring would act as a queue of sorts for requests to perform sanction searches and then there would be some sort of scheduled job that would go through and perform the searches and update the records with the results using a local database built from the data that OFAC provides.

## The Solution

Like I mentioned, Onspring is flexible and that is what I wanted to highlight with this solution. It provides a lot of functionality to take care of collecting the data needed to perform the search and then storing the results. It also means that once the results are stored in Onspring I can use the platform to build out any number of other processes that might need to be triggered based on them. For example, if a search comes back positive for a match I could trigger an an email to be sent to the person who initiated the search as well as the person who is responsible for doing additional due diligence. In Onspring then what I needed was two apps, one to store the requests for searches and one to store the results of the searches. The results would then be related back to the requests via a reference field.

### The Search Requests App

The search requests app is super simple. It has a single-line text field for the name of the entity being searched, a status field to track the status of the search, a multi-line text field that is used to collect any error that could occur while performing the search, and a parallel reference field to the search results app. The status field is used to identify if the search is awaiting processing, being processed, or has been completed successfully or with an error. Here is a quick look at a single record in the search request app.

![Search Request App](posts/automating-sanctions-checks-with-onspring/search-requests-app.png)

Again this app could be used to manually perform searches or someone could just setup a trigger that creates a record into the app whenever a search needs to be performed, copy the appropriate name value, and then let the scheduled job take care of the rest. In this way it is meant to act very much like a queue of requests to be processed. Any app or survey could be tied into this app via a reference field to trigger a search.

> [!NOTE]
> I'm just searching for the entity based on name, but you could most definitely extend this to search based on other fields like address, type, program, etc. You'd just need to add fields to hold that information and make sure the glue code is updated to use that information when performing the search.

### The Search Results App

The search results app is also rather simple. It basically just has the fields necessary to display the relevant information about an entity that was found to be a match in the search. In my case I decided to just store the name of the entity, the type of the entity, the program that the entity is sanctioned under, and the first address that is listed for the entity. Here is a quick look at a single record in the search results app.

![Search Results App](posts/automating-sanctions-checks-with-onspring/search-results-app.png)

Again one of the great things about putting these results back into Onspring is that you can then use these results in whatever other processes you might be managing there.

At this point a lot of the work is done. We have a way to queue up requests to perform searches and a way to store the results of those searches. The only thing left to do is to write the glue code that will pull the queue of requests, perform the searches, and then store the results back into Onspring.

> [!NOTE]
> I'm just surfacing the information in the results that I thought would be helpful. You could most definitely extend this to store more information about the entity that was found to be a match. You'd just need to add fields to hold that information and make sure the glue code is updated to use that information when storing the results.

### The Glue Code

I don't want to get too much into the specifics of my implementation because in reality this part of the solution could look a lot different based on your specific needs and the resources you have available to you to build it. I'll cover the basics of what I did, but if you want to dive in deeper or actually take my solution for a spin you can find it all in this [GitHub repository](https://github.com/StevanFreeborn/sanctions-search/tree/main) with all the relevant documentation.

Remember that I wanted to build a plugin like solution that anyone could tie into within an Onspring instance if they wanted to perform sanctions checks against the SDN list. However, Onspring doesn't explicitly provide a way to perform searches like this even if I did have the SDN data in Onspring. But I can extend Onspring to do this because they have a great public [API](https://onspringapidocs.stevanfreeborn.com) that I can use to pull data from Onspring into my own code that can perform the searches and then push the results back into Onspring.

My language and platform of choice most of the time for this type of work is C# and .NET. Plus Onspring has a [SDK](https://github.com/onspring-technologies/onspring-api-sdk) available for C# making the integration with their API even more convenient.

> [!NOTE]
> I also have built and maintain a similar SDK for those partial to Python and/or JS/TS. You can find the Python SDK [here](https://github.com/StevanFreeborn/onspring-api-sdk-python) and the JS/TS SDK [here](https://github.com/StevanFreeborn/onspring-api-sdk-javascript).

So I decided to build a worker service that in itself contains two different workers. One worker is responsible for pulling all the necessary data from OFAC and building a local SQLite database that can be used to perform searches against. This worker runs when the service starts and then every hour after (this interval is configurable) to keep the local database up to date as OFAC publishes new data.

The other worker is responsible for pulling the queue of requests from Onspring, performing the searches, and then storing the results back into Onspring. This worker runs every 1 minutes (this interval is configurable) to retrieve all available requests that are awaiting processing. This worker also updates the status of the request in Onspring as it progresses through the search. It's probably also worth noting that the worker responsible for building the local database always runs at least once before the worker responsible for performing the searches to make sure the local database is actually in existence.

When all is said and done, the code that does all the work looks like this::

```csharp
private async Task RunSearchRequests()
{
  var searchBatchIdProperty = LogContext.PushProperty("SearchBatchId", Guid.NewGuid());

  _logger.LogInformation("Running search requests");

  try
  {
    using var scope = _serviceScopeFactory.CreateAsyncScope();
    var onspringService = scope.ServiceProvider.GetRequiredService<IOnspringService>();
    var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

    var searchRequests = await onspringService.GetSearchRequestsAsync();

    if (searchRequests.Count is 0)
    {
      _logger.LogInformation("No search requests found");
      return;
    }

    foreach (var searchRequest in searchRequests)
    {
      var searchBatchItemIdProperty = LogContext.PushProperty("SearchBatchItemId", Guid.NewGuid());

      try
      {
        _logger.LogInformation("Processing search request {SearchRequestId}", searchRequest.Id);

        await onspringService.UpdateSearchRequestAsProcessingAsync(searchRequest);
        var searchResult = await searchService.PerformSearchAsync(searchRequest);
        await onspringService.AddSearchResultAsync(searchResult);

        _logger.LogInformation("Search request {SearchRequestId} processed", searchRequest.Id);
      }
      catch (Exception ex)
      {
        await onspringService.UpdateSearchRequestAsFailedAsync(
          searchRequest,
          $"Failed to process search request: {ex.Message}"
        );

        _logger.LogError(ex, "Failed to process search request {SearchRequestId}", searchRequest.Id);
      }
      finally
      {
        searchBatchItemIdProperty.Dispose();
      }
    }
  }
  catch (Exception ex)
  {
    _logger.LogError(ex, "Failed to run search requests");
  }
  finally
  {
    searchBatchIdProperty.Dispose();
  }
}
```

Once you've got the code written and tested you just have to deploy, configure, and run it somewhere. The specifics of this will vary depending on your situation, but I set this worker up so that it can be run as a [Docker](https://www.docker.com) container, a windows service, a linux service, or just as an executable.

## The Result

Once I've got the worker running and my apps configured in Onspring I'm now able to queue up requests to perform searches and then have those searches performed automatically. The results are then stored back into Onspring and can be used in any number of other processes that I might be managing there. Here is a quick look at it in action.

![Performing Searches](posts/automating-sanctions-checks-with-onspring/performing-searches.gif)

Granted as with anything you build there is going to be testing required to make sure it is working as expected, to iron out edge cases, and to make sure that it is meeting the needs of the users. But I think this is a great example of how you can use Onspring to automate a process that might be being done manually today even if the exact functionality you need isn't provided out of the box.

## Conclusion

And there you have it - a practical example of Onspring's flexibility in action. We've transformed a complex compliance task into an automated, efficient process using Onspring as our foundation. But remember, this isn't just about sanctions checks. It's about recognizing the potential in the platform you already have. Whether it's vendor management, incident response, or any other process, Onspring gives you the building blocks to create custom solutions that meet your needs.
