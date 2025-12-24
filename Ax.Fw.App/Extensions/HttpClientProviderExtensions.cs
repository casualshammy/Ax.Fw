using Ax.Fw.App.Interfaces;
using Ax.Fw.App.Modules.HttpClientProvider;

namespace Ax.Fw.App.Extensions;

public static class HttpClientProviderExtensions
{
  /// <summary>
  /// Registers an <see cref="IHttpClientProvider"/> implementation with the application, optionally configuring default
  /// HTTP headers.
  /// </summary>
  /// <remarks>This method adds an <see cref="IHttpClientProvider"/> to the application's dependency injection
  /// container, making it available for use throughout the application's lifetime. If <paramref
  /// name="_defaultHeaders"/> is provided, these headers will be included in all HTTP requests made via the
  /// provider.</remarks>
  /// <param name="_appBase">The application base instance to which the HTTP client provider will be added.</param>
  /// <param name="_defaultHeaders">An optional read-only dictionary containing default HTTP headers to be applied to all outgoing requests. If <see
  /// langword="null"/>, no default headers are configured.</param>
  /// <returns>The <see cref="AppBase"/> instance with the HTTP client provider registered as a singleton service.</returns>
  public static AppBase UseHttpClient(
    this AppBase _appBase,
    IReadOnlyDictionary<string, string>? _defaultHeaders)
  {
    var httpClientProvider = new HttpClientProviderImpl(_defaultHeaders);

    return _appBase
      .AddSingleton<HttpClientProviderImpl, IHttpClientProvider>(httpClientProvider);
  }
}
