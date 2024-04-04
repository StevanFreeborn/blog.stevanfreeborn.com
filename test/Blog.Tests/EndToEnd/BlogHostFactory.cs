using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Blog.Tests.EndToEnd;

public class BlogHostFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
  private IHost? _host;

  private void EnsureServer()
  {
    if (_host is null)
    {
      using var _ = CreateDefaultClient();
    }
  }

  protected override void Dispose(bool disposing)
  {
    _host?.Dispose();
  }

  public string ServerAddress
  {
    get
    {
      EnsureServer();
      return ClientOptions.BaseAddress.ToString();
    }
  }

  protected override IHost CreateHost(IHostBuilder builder)
  {    
    var testHost = builder
      .ConfigureWebHost(
        webHostBuilder => webHostBuilder.ConfigureLogging(
          config => config.ClearProviders()
        )
      )
      .Build();

    builder.ConfigureWebHost(
      webHostBuilder => webHostBuilder.UseKestrel(
        o => o.Listen(IPAddress.Loopback, 0)
      )
    );

    _host = builder.Build();  
    _host.Start();  

    var server = _host.Services.GetRequiredService<IServer>();  
    var addresses = server.Features.Get<IServerAddressesFeature>();  

    ClientOptions.BaseAddress = addresses!.Addresses  
      .Select(x => new Uri(x))  
      .Last();

    testHost.Start();  
    return testHost;  
  }
}