namespace Blog.Tests.EndToEnd;

public class BlogPageTest : PageTest
{
  private readonly BlogHostFactory<Program> _factory = new();

  public override BrowserNewContextOptions ContextOptions()
  {
    var baseUrl = "http://localhost:5000";

    _factory
      .WithWebHostBuilder(builder => builder.UseUrls(baseUrl))
      .CreateDefaultClient();

    var options = base.ContextOptions();
    options.BaseURL = baseUrl;
    return options;
  }
}