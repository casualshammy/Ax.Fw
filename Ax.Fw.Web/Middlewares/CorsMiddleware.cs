using Microsoft.AspNetCore.Http;

namespace Ax.Fw.Web.Middlewares;

public class CorsMiddleware : IMiddleware
{
  private readonly IReadOnlySet<string> p_allowedOrigins;
  private readonly string p_allowedMethods;
  private readonly string p_allowedHeaders;
  private readonly bool p_allowedCredentials;

  internal CorsMiddleware(
    IEnumerable<string> _allowedOrigins,
    IEnumerable<string> _allowedMethods,
    IEnumerable<string> _allowedHeaders,
    bool _allowedCredentials)
  {
    p_allowedOrigins = _allowedOrigins.ToHashSet();
    p_allowedMethods = string.Join(", ", _allowedMethods);
    p_allowedHeaders = string.Join(", ", _allowedHeaders);
    p_allowedCredentials = _allowedCredentials;
  }

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
