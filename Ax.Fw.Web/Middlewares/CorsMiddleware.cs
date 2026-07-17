using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;

namespace Ax.Fw.Web.Middlewares;

/// <summary>
/// Middleware for handling Cross-Origin Resource Sharing (CORS) requests.
/// </summary>
/// <remarks>
/// This middleware processes CORS preflight requests (OPTIONS) and adds appropriate
/// CORS headers to all responses. It validates the request origin against a configured
/// list of allowed origins and only adds CORS headers for allowed origins.
/// </remarks>
public class CorsMiddleware : IMiddleware
{
  private readonly FrozenSet<string> p_allowedOrigins;
  private readonly string p_allowedMethods;
  private readonly string p_allowedHeaders;
  private readonly bool p_allowedCredentials;

  /// <summary>
  /// Initializes a new instance of the <see cref="CorsMiddleware"/> class.
  /// </summary>
  /// <param name="_allowedOrigins">Collection of allowed origins. Use "*" to allow all origins.</param>
  /// <param name="_allowedMethods">Collection of allowed HTTP methods.</param>
  /// <param name="_allowedHeaders">Collection of allowed request headers.</param>
  /// <param name="_allowedCredentials">Whether to allow credentials (cookies, authorization headers) in cross-origin requests.</param>
  internal CorsMiddleware(
    IEnumerable<string> _allowedOrigins,
    IEnumerable<string> _allowedMethods,
    IEnumerable<string> _allowedHeaders,
    bool _allowedCredentials)
  {
    p_allowedOrigins = _allowedOrigins.ToFrozenSet();
    p_allowedMethods = string.Join(", ", _allowedMethods);
    p_allowedHeaders = string.Join(", ", _allowedHeaders);
    p_allowedCredentials = _allowedCredentials;
  }

  /// <summary>
  /// Invokes the middleware to process the HTTP request.
  /// </summary>
  /// <param name="_httpCtx">The HTTP context for the current request.</param>
  /// <param name="_next">The delegate to invoke the next middleware in the pipeline.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  public async Task InvokeAsync(
    HttpContext _httpCtx,
    RequestDelegate _next)
  {
    var req = _httpCtx.Request;
    var origin = req.Headers.Origin.ToString();
    string corsOrigin;
    if (p_allowedOrigins.Contains("*") || p_allowedOrigins.Contains(origin))
      corsOrigin = origin;
    else
      corsOrigin = "not-allowed.local";

    if (req.Method == "OPTIONS")
    {
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Origin", corsOrigin);
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Methods", p_allowedMethods);
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Headers", p_allowedHeaders);
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Credentials", p_allowedCredentials ? "true" : "false");
      _httpCtx.Response.Headers.Append("Access-Control-Max-Age", "1728000");
      _httpCtx.Response.Headers.Append("Content-Type", "text/plain; charset=utf-8");
      _httpCtx.Response.Headers.ContentLength = 0;
      _httpCtx.Response.StatusCode = 204;
      return;
    }

    _httpCtx.Response.Headers.Append("Access-Control-Allow-Origin", corsOrigin);
    _httpCtx.Response.Headers.Append("Access-Control-Allow-Methods", p_allowedMethods);
    _httpCtx.Response.Headers.Append("Access-Control-Allow-Headers", p_allowedHeaders);
    _httpCtx.Response.Headers.Append("Access-Control-Allow-Credentials", p_allowedCredentials ? "true" : "false");
    //_httpCtx.Response.Headers.Append("Access-Control-Expose-Headers", "Content-Length,Content-Range");
    await _next(_httpCtx);
  }

}
