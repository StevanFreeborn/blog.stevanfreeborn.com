using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Blog.Tests.EndToEnd;

[TestFixture]
public class BlogTest : PageTest
{
  private readonly BlogHostFactory<Program> _factory = new();

  public override BrowserNewContextOptions ContextOptions()
  {
    var options = base.ContextOptions();
    options.BaseURL = _factory.ServerAddress;
    return options;
  }
}