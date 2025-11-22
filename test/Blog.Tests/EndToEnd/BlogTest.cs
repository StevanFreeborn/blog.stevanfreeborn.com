namespace Blog.Tests.EndToEnd;

[TestFixture]
public abstract class BlogTest : PageTest
{
  private readonly BlogHostFactory<Program> _factory = new();

  public override BrowserNewContextOptions ContextOptions()
  {
    var options = base.ContextOptions();
    options.BaseURL = _factory.ServerAddress;
    return options;
  }

  [SetUp]
  public async Task Setup()
  {
    await Context.Tracing.StartAsync(new()
    {
      Title = TestContext.CurrentContext.Test.ClassName + "." + TestContext.CurrentContext.Test.Name,
      Screenshots = true,
      Snapshots = true,
      Sources = true
    });
  }

  [TearDown]
  public async Task TearDown()
  {
    await Context.Tracing.StopAsync(new()
    {
      Path = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "playwright-traces",
            $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}.zip"
        )
    });
  }
}