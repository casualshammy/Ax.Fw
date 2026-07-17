using Ax.Fw.Extensions;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Ax.Fw.Web.Middlewares;

/// <summary>
/// Middleware that extracts the real client IP address from proxy headers
/// when the application is running behind a reverse proxy or load balancer.
/// </summary>
/// <remarks>
/// This middleware checks for the following headers in order:
/// <list type="number">
/// <item><description><c>CF-Connecting-IP</c> - Used by Cloudflare to pass the original client IP.</description></item>
/// <item><description><c>X-Forwarded-For</c> - Standard header used by most proxies. The first IP in the comma-separated list is used.</description></item>
/// </list>
/// If a valid IP address is found in either header, it replaces <see cref="HttpConnection.RemoteIpAddress"/> 
/// so that downstream middleware and controllers see the real client IP instead of the proxy's IP.
/// </remarks>
public sealed class ForwardProxyMiddleware : IMiddleware
{
  public ForwardProxyMiddleware()
  { }

  public async Task InvokeAsync(HttpContext _ctx, RequestDelegate _next)
  {
    if (_ctx.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfConnectingIp))
    {
      var headerValue = cfConnectingIp.ToString();
      if (!headerValue.IsNullOrWhiteSpace() && IPAddress.TryParse(headerValue, out var ip))
      {
        _ctx.Connection.RemoteIpAddress = ip;
        await _next(_ctx);
        return;
      }
    }

    if (_ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
    {
      var headerValue = xForwardedFor.ToString();
      if (!headerValue.IsNullOrWhiteSpace())
      {
        var split = headerValue.Split(',', StringSplitOptions.TrimEntries);
        if (split.Length > 0 && IPAddress.TryParse(split[0], out var ip))
        {
          _ctx.Connection.RemoteIpAddress = ip;
          await _next(_ctx);
          return;
        }
      }
    }

    await _next(_ctx);
  }
}
