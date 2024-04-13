namespace Blog.Tests.EndToEnd;

public class CompositeHost(IHost testHost, IHost kestrelHost) : IHost
{
  private readonly IHost _testHost = testHost;
  private readonly IHost _kestrelHost = kestrelHost;

  public IServiceProvider Services => _testHost.Services;

  public void Dispose()
  {
    _testHost.Dispose();
    _kestrelHost.Dispose();
    GC.SuppressFinalize(this);
  }

  public async Task StartAsync(CancellationToken cancellationToken = default)
  {
    await _testHost.StartAsync(cancellationToken);
    await _kestrelHost.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken = default)
  {
    await _testHost.StopAsync(cancellationToken);
    await _kestrelHost.StopAsync(cancellationToken);
  }
}