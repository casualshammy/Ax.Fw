using Ax.Fw.Extensions;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Ax.Fw.Web.Middlewares;

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
