using Ax.Fw.App.Interfaces;

namespace Ax.Fw.App.Modules.HttpClientProvider;

internal class HttpClientProviderImpl : IHttpClientProvider
{
  public HttpClientProviderImpl(
    IReadOnlyDictionary<string, string>? _defaultHeaders)
  {
    var handler = new SocketsHttpHandler
    {
      PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    };
    var httpClient = new HttpClient(handler);
    httpClient.Timeout = TimeSpan.FromSeconds(10);

    if (_defaultHeaders != null)
      foreach (var (key, value) in _defaultHeaders)
        httpClient.DefaultRequestHeaders.Add(key, value);

    HttpClient = httpClient;
  }

  public HttpClient HttpClient { get; }

}
