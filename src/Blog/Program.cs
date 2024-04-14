using Blog.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOptions<FilePostServiceOptionsSetup>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddScoped<IPostService, FilePostService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents();

var app = builder.Build();

if (app.Environment.IsProduction())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseStatusCodePagesWithRedirects("/Error/{0}");

app
  .MapGet("/rss", async (HttpContext context, IPostService postService) =>
  {
    var baseUrl = context.Request.GetBaseUrl();
    var url = $"{baseUrl}{context.Request.PathBase}";

    var posts = await postService.GetPostsAsync();

    var items = posts.Select(post =>
    {
      var uri = new Uri($"{url}/{post.Slug}");

      var item = new SyndicationItem(
        post.Title,
        post.Lead,
        uri,
        post.Slug,
        post.PublishedAt
      );

      return item;
    });

    var feed = new SyndicationFeed(
      "journal",
      "A blog by Stevan Freeborn",
      new Uri(url)
    )
    {
      Items = items,
    };

    XNamespace atom = "http://www.w3.org/2005/Atom";
    feed.ElementExtensions.Add(
      new XElement(atom + "link",
        new XAttribute("href", url + "/rss"),
        new XAttribute("rel", "self"),
        new XAttribute("type", "application/rss+xml")
      )
    );

    var settings = new XmlWriterSettings
    {
      Encoding = Encoding.UTF8,
      NewLineHandling = NewLineHandling.Entitize,
      Indent = true,
      Async = true,
    };

    using var stream = new MemoryStream();
    using var writer = XmlWriter.Create(stream, settings);

    var rssFormatter = new Rss20FeedFormatter(feed, false);
    rssFormatter.WriteTo(writer);
    await writer.FlushAsync();

    return Results.File(stream.ToArray(), "application/xml");
  })
  .WithDisplayName("RSS Feed")
  .WithDescription("RSS feed for the blog");

app.MapRazorComponents<App>();

app.Run();

public partial class Program { }