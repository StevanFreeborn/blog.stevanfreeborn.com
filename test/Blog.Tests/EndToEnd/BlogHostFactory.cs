namespace Blog.Tests.EndToEnd;

public class BlogHostFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
  protected override IHost CreateHost(IHostBuilder builder)
  {
    var testHost = base.CreateHost(builder);
    builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());
    
    var kestrelHost = builder.Build();
    kestrelHost.Start();
    
    return new CompositeHost(testHost, kestrelHost);
  }
}