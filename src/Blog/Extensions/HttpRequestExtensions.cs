namespace Blog.Extensions;
public static class HttpRequestExtensions
{
  public static string GetBaseUrl(this HttpRequest request)
  {
    var scheme = request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? request.Scheme;
    var host = request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? request.Host.Value;
    return $"{scheme}://{host}";
  }
}