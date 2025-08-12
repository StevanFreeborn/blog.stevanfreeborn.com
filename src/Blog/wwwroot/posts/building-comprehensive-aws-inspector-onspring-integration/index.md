```json meta
{
  "title": "Building a Comprehensive AWS Inspector and Onspring Integration for Better Findings Management",
  "lead": "Learn how to build a robust integration between AWS Inspector and Onspring to automatically sync findings and resources, allowing to utilize and reference this data in your GRC processes",
  "isPublished": true,
  "publishedAt": "2025-08-12",
  "openGraphImage": "posts/building-comprehensive-aws-inspector-onspring-integration/og-image.png"
}
```

Recently, I had the opportunity to work on a project that involved integrating [AWS Inspector](https://aws.amazon.com/inspector/) with [Onspring](https://onspring.com/) to allow users to reference their resource and findings data within their GRC processes. The goal was to automatically synchronize findings from AWS Inspector with Onspring, allowing teams to track, manage, and remediate findings across their AWS infrastructure within their new or existing processes in Onspring. What made this project particularly interesting was the combination of scheduled resource monitoring, on-demand findings syncing, and the integration patterns needed to handle large-scale AWS environments.

> [!NOTE]
> Keep in mind that this content reflects my recent experience with AWS Inspector and Onspring APIs. The interfaces or exact steps might vary slightly as AWS and Onspring update their platforms. When in doubt, refer to their official documentation for the most accurate information.

> [!NOTE]
> You can find all the code for this integration [here](https://github.com/StevanFreeborn/awsinspector-poc).

## Technologies Used

For this integration, I chose to build it using [.NET 9](https://dotnet.microsoft.com/) and [C#](https://docs.microsoft.com/en-us/dotnet/csharp/), taking advantage of the excellent AWS SDK and modern C# features. The application is built as an ASP.NET Core web API that provides both scheduled background processing and an API endpoint for findings synchronization.

Key technologies and packages used include:

- **.NET 9** with ASP.NET Core for the web API framework
- **AWS SDK for .NET** (`AWSSDK.Inspector2` and `AWSSDK.ResourceExplorer2`) for interacting with AWS services
- **Onspring.API.SDK** for seamless integration with the Onspring platform
- **OpenTelemetry** for comprehensive observability and monitoring
- **Docker** for containerization and deployment
- **GitHub Actions** for CI/CD automation

The application is designed to run as a containerized service, making it easy to deploy anywhere from on-premises servers to cloud platforms like AWS ECS or Azure Container Instances.

## Overview of the Integration

The integration serves as a bridge between AWS Inspector's capability to scan intrastructure and report findings and Onspring's GRC platform. It operates in two main modes:

1. **Scheduled Resource Discovery**: Automatically discovers and syncs AWS resources to Onspring on a configurable interval
2. **On-Demand Vulnerability Sync**: Provides an API endpoint that can be called (potentially from Onspring's REST API outcomes) to sync findings for specific resources

The integration handles the complex mapping between AWS Inspector's data model and Onspring's flexible field structure, ensuring that teams have all the context they need to prioritize and remediate findings effectively for their AWS infrastructure within the context of their existing or new GRC processes.

What sets this apart from simple ETL solutions is the intelligent queuing system, parallel processing capabilities, and the ability to maintain relationships between resources and their associated findings. This allows teams to creates a comprehensive dashboard within Onspring that stays up-to-date with the latest findings from AWS Inspector.

## Building the Integration

### Setting Up the Core Architecture

I started by creating a relatively simple architecture that tries to maintain a clear separation of concerns. The application is structured more or less into these layers:

- **Controllers/Endpoints**: Handle HTTP requests and responses
- **Services**: Contain business logic for AWS and Onspring interactions. Act as facades for the underlying SDKs from each platform.
- **Models**: Define data structures for AWS findings and resources
- **Background Services**: Handle scheduled tasks and queue processing
- **Configuration**: Manage complex configuration options for both AWS and Onspring

The main program setup establishes all the necessary services and configures the application:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddTelemetry();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient();

// AWS services
builder.Services.ConfigureOptions<AwsOptionsSetup>();
builder.Services.AddSingleton<IAwsResourceService, AwsResourceService>();
builder.Services.AddSingleton<IAwsInspectorService, AwsInspectorService>();

// Onspring integration
builder.Services.ConfigureOptions<OnspringOptionsSetup>();
builder.Services.AddSingleton<IOnspringService, OnspringService>();

// Background services
builder.Services.ConfigureOptions<ResourceMonitorOptionsSetup>();
builder.Services.AddHostedService<ResourceMonitor>();
builder.Services.AddSingleton<ISyncFindingsQueue, SyncFindingsQueue>();
builder.Services.AddHostedService<SyncFindingsQueueProcessor>();
```

### Implementing AWS Resource Discovery

One of the core challenges was efficiently discovering AWS resources across multiple regions. I used AWS Resource Explorer 2 to get a comprehensive view of all resources:

```csharp
public async IAsyncEnumerable<AwsResource> GetResourcesAsync()
{
  foreach (var region in _awsOptions.CurrentValue.Regions)
  {
    var regionEndpoint = RegionEndpoint.GetBySystemName(region);
    using var resourceClient = new AmazonResourceExplorer2Client(_credentials, regionEndpoint);
    var request = new ListResourcesRequest();
    var paginator = resourceClient.Paginators.ListResources(request);

    await foreach (var resource in paginator.Resources)
    {
      yield return new AwsResource
      {
        Arn = resource.Arn,
        Type = resource.ResourceType,
      };
    }
  }
}
```

The use of `IAsyncEnumerable` here is great because it allows for handling large AWS environments efficiently, as it allows processing resources as they're discovered rather than loading everything into memory at once.

### Building the AWS Inspector Integration

The AWS Inspector service handles the complex task of retrieving findings for specific resources. This involves filtering findings by resource ID and handling pagination across multiple regions:

```csharp
public async IAsyncEnumerable<AwsFinding> GetFindingsForResourceAsync(string resourceArn)
{
  foreach (var region in _options.CurrentValue.Regions)
  {
    var regionEndpoint = RegionEndpoint.GetBySystemName(region);
    using var inspectorClient = new AmazonInspector2Client(_credentials, regionEndpoint);
    var request = new ListFindingsRequest
    {
      FilterCriteria = new()
      {
        ResourceId = [
          new()
          {
            Comparison = StringComparison.EQUALS,
            Value = resourceArn.Split('/').Last()
          }
        ]
      }
    };
    var paginator = inspectorClient.Paginators.ListFindings(request);

    await foreach (var finding in paginator.Findings)
    {
      yield return new AwsFinding
      {
        Arn = finding.FindingArn,
        Title = finding.Title,
        Description = finding.Description,
        Severity = finding.Severity,
        Status = finding.Status,
        InspectorScore = finding.InspectorScore,
        RemediationRecommendation = finding.Remediation.Recommendation.Text,
        // ... additional fields
      };
    }
  }
}
```

> [!NOTE]
> I love how the AWS SDK handles retrieving resources from paged endpoints. Definitely going to remember this approach when developing my own APIs.

### Creating the Onspring Integration Layer

The Onspring service handles the complex mapping between AWS data and Onspring's field structure. I'm not terribly happy with the approach I've taken as it doesn't scale well if you have a lot more fields you need to ingest data into, but it is straightforward and easy to understand. One of the most interesting aspects is how it manages relationships between resources and findings:

```csharp
public async Task AddOrUpdateFindingAsync(string resourceArn, AwsFinding finding)
{
  // First, check if the finding already exists
  var queryRequest = new QueryRecordsRequest()
  {
    AppId = _options.CurrentValue.VulnerabilitiesAppId,
    Filter = $"{_options.CurrentValue.VulnerabilitiesAwsArnFieldId} eq '{finding.Arn}'"
  };

  var queryResult = await _onspringClient.QueryRecordsAsync(queryRequest);
  var existingRecordId = queryResult.Value.Items.FirstOrDefault()?.RecordId ?? 0;
  var onspringResourceRecordId = await GetResourceRecordIdAsync(resourceArn);

  // Build the record with all field mappings
  var onspringRecord = new ResultRecord()
  {
    AppId = _options.CurrentValue.VulnerabilitiesAppId,
    RecordId = existingRecordId,
    FieldData = [
      new StringFieldValue(_options.CurrentValue.VulnerabilitiesAwsArnFieldId, finding.Arn),
      new StringFieldValue(_options.CurrentValue.VulnerabilitiesNameFieldId, finding.Title),
      new StringFieldValue(_options.CurrentValue.VulnerabilitiesAwsSeverityFieldId, finding.Severity),
      new DecimalFieldValue(_options.CurrentValue.VulnerabilitiesAwsInspectorScoreFieldId,
        Convert.ToDecimal(finding.InspectorScore, CultureInfo.InvariantCulture)),
      new IntegerListFieldValue(_options.CurrentValue.VulnerabilitiesBusinessApplicationsFieldId,
        [onspringResourceRecordId])
    ]
  };

  var saveResult = await _onspringClient.SaveRecordAsync(onspringRecord);
}
```

### Implementing Background Processing

To handle the scale of AWS environments, I implemented two background services:

1. **ResourceMonitor**: Continuously discovers and syncs AWS resources
2. **SyncFindingsQueueProcessor**: Processes findings sync requests asynchronously

The ResourceMonitor uses parallel processing to handle multiple resources simultaneously:

```csharp
private async Task CreateOrUpdateResourcesAsync()
{
  _logger.LogInformation("Processing resources...");
  var startTimeStamp = _timeProvider.GetTimestamp();
  var count = 0;

  await Parallel.ForEachAsync(
    _awsResourceService.GetResourcesAsync(),
    async (resource, _) =>
    {
      Interlocked.Increment(ref count);

      try
      {
        await _onspringService.AddOrUpdateResourceAsync(resource);
        _logger.LogInformation("Successfully processed resource: {ResourceArn}", resource.Arn);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to process resource: {ResourceArn}", resource.Arn);
      }
    }
  );

  var elapsedTime = _timeProvider.GetElapsedTime(startTimeStamp);
  _logger.LogInformation("Finished processing {Count} resources in {ElapsedTime} ms", count, elapsedTime.TotalMilliseconds);
}
```

It is also probably important to say that I made the decision to handle processing sync requests in the background because Onspring's REST API outcome has a timeout limit which depending on the resource the sync request targets it is possible that the processing time to sync all of the findings could exceed that limit. By using a queue and processing the requests in the background we can avoid ever having to concern ourselves with that timeout.

### Adding API Endpoints for On-Demand Syncing

The integration provides a REST API endpoint that can be called to trigger findings syncing for specific resources:

```csharp
app
  .MapPost("/sync-findings", async (
    [FromBody] SyncFindingsRequest request,
    [FromServices] ISyncFindingsQueue queue,
    [FromServices] TimeProvider timeProvider
  ) =>
  {
    if (request.IsValid() is false)
    {
      return Results.BadRequest($"Invalid request: {nameof(request.ResourceArn)} should not be empty.");
    }

    await queue.EnqueueAsync(SyncFindingsQueueItem.From(request));

    return Results.Ok(new { DateSyncRequested = timeProvider.GetUtcNow() });
  })
  .RequireAuthorization(BasicAuthentication.SchemeName);
```

This endpoint uses basic authentication and queues the sync request for asynchronous processing, ensuring that API calls return quickly even when processing large numbers of findings.

### Configuration and Deployment

The application uses a comprehensive configuration system that maps all the necessary field IDs and settings for both AWS and Onspring:

```json
{
  "AwsOptions": {
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "Regions": ["us-east-1", "us-west-2"]
  },
  "OnspringOptions": {
    "BaseUrl": "https://your-instance.onspring.com",
    "ApiKey": "your-api-key",
    "BusinessApplicationAppId": 123,
    "VulnerabilitiesAppId": 456,
    "VulnerabilitiesAwsArnFieldId": 789
    // ... many more field mappings
  },
  "ResourceMonitorOptions": {
    "Enabled": true,
    "PollingInterval": "01:00:00"
  }
}
```

For deployment, I created a multi-stage Dockerfile that helps improve the build process by allowing certain layers to be cached:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app

COPY *.sln ./
COPY src/AwsInspectorPoc.API/*.csproj src/AwsInspectorPoc.API/
COPY tests/AwsInspectorPoc.API.Tests/*.csproj tests/AwsInspectorPoc.API.Tests/

RUN dotnet restore AwsInspectorPoc.sln

COPY . .

FROM base AS publish-stage
RUN dotnet publish -c Release -o dist src/AwsInspectorPoc.API/AwsInspectorPoc.API.csproj

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish-stage /app/dist ./
ENTRYPOINT ["dotnet", "AwsInspectorPoc.API.dll"]
```

### Adding Observability

To ensure I have the ability to monitor and troubleshoot the integration once deployed, I added some observability using OpenTelemetry:

```csharp
builder.AddTelemetry();
```

This provides distributed tracing and logging that can be exported to various observability platforms. The application includes logging throughout the processing pipeline, making it possible to troubleshoot issues and monitor the integration once it is no longer running locally.

## Key Benefits and Outcomes

This integration provides several significant benefits:

1. **Centralized Findings Management**: Teams can view all AWS Inspector findings alongside other data in Onspring
2. **Automated Relationship Mapping**: Findings are automatically linked to their associated AWS resources
3. **Real-time Synchronization**: The API endpoint allows for immediate findings syncing only when needed.
4. **Scalable Processing**: Parallel processing and background processing ensure the integration can handle large AWS environments
5. **Comprehensive Observability**: Built-in telemetry provides visibility into integration performance and reliability

## Lessons Learned

A few things I took away after working on this project:

1. **Async Enumerable is Powerful**: Using `IAsyncEnumerable` for processing large datasets from AWS APIs significantly improves memory efficiency and developer experience
2. **Configuration Complexity**: Integrations with Onspring require careful consideration about how you are going to manage mapping data from another system to fields in Onspring
3. **Background Processing Design**: Separating processing a request from receiving and acknowleding sync requests provides flexibility to handle small and large datasets

## Conclusion

Building this AWS Inspector and Onspring integration was a fun challenge that demonstrates the joy of using modern .NET for building integrations. The combination of scheduled resource discovery, on-demand findings syncing, and observability creates a solution that can scale with growing AWS environments while providing teams with the centralized visibility they need.

The event-driven capabilities, combined with traditional scheduled processing, provide flexibility for different organizational needs. Whether you need continuous monitoring or triggered synchronization based on events in Onspring, this architecture can support both approaches.

I hope this walkthrough has been helpful in showing how you can build integrations between AWS services and the Onspring platform.
