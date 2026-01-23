using Ax.Fw.SharedTypes.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Ax.Fw.Web.Middlewares;

public class CorsMiddleware : IMiddleware
{
  private readonly ILog p_log;
  private readonly string p_allowedOrigins;
  private readonly string p_allowedMethods;
  private readonly string p_allowedHeaders;
  private readonly bool p_allowedCredentials;

  internal CorsMiddleware(
    ILog _log,
    IReadOnlySet<string> _allowedOrigins,
    IReadOnlySet<string> _allowedMethods,
    IReadOnlySet<string> _allowedHeaders,
    bool _allowedCredentials)
  {
    p_log = _log;
    p_allowedOrigins = string.Join(", ", _allowedOrigins);
    p_allowedMethods = string.Join(", ", _allowedMethods);
    p_allowedHeaders = string.Join(", ", _allowedHeaders);
    p_allowedCredentials = _allowedCredentials;
  }

  public async Task InvokeAsync(
    HttpContext _httpCtx,
    RequestDelegate _next)
  {
    if (_httpCtx.Request.Method == "OPTIONS")
    {
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Origin", p_allowedOrigins);
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Methods", p_allowedMethods);
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Headers", p_allowedHeaders);
      _httpCtx.Response.Headers.Append("Access-Control-Allow-Credentials", p_allowedCredentials ? "true" : "false");
      _httpCtx.Response.Headers.Append("Access-Control-Max-Age", "1728000");
      _httpCtx.Response.Headers.Append("Content-Type", "text/plain; charset=utf-8");
      _httpCtx.Response.Headers.ContentLength = 0;
      _httpCtx.Response.StatusCode = 204;
      return;
    }

    _httpCtx.Response.Headers.Append("Access-Control-Allow-Origin", p_allowedOrigins);
    _httpCtx.Response.Headers.Append("Access-Control-Allow-Methods", p_allowedMethods);
    _httpCtx.Response.Headers.Append("Access-Control-Allow-Headers", p_allowedHeaders);
    _httpCtx.Response.Headers.Append("Access-Control-Allow-Credentials", p_allowedCredentials ? "true" : "false");
    //_httpCtx.Response.Headers.Append("Access-Control-Expose-Headers", "Content-Length,Content-Range");
    await _next(_httpCtx);
  }

}
