using Ax.Fw.Extensions;
using Ax.Fw.Web.Data;
using Ax.Fw.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net;

namespace Ax.Fw.Web.Middlewares;

public class LogMiddleware : IMiddleware
{
  private static long p_reqCount = -1;
  private readonly IScopedLog p_log;

  public LogMiddleware(
    IScopedLog _log)
  {
    p_log = _log;
  }

  public async Task InvokeAsync(
    HttpContext _context,
    RequestDelegate _next)
  {
    var request = _context.Request;
    var reqIndex = Interlocked.Increment(ref p_reqCount);

    p_log.Info($"[{reqIndex}] --> **{request.Method}** __{request.Path}__");

    var sw = Stopwatch.StartNew();
    await _next(_context);

    var problemDetails = _context.Features.Get<ProblemFeature>()?.Details;
    if (problemDetails.IsNullOrWhiteSpace())
      p_log.Info($"[{reqIndex}] <-- **{request.Method}** __{request.Path}__ **{(HttpStatusCode)_context.Response.StatusCode}** (__{sw.ElapsedMilliseconds} ms__)");
    else
      p_log.Info($"[{reqIndex}] <-- **{request.Method}** __{request.Path}__ **{(HttpStatusCode)_context.Response.StatusCode}** (__{sw.ElapsedMilliseconds} ms__) ({problemDetails})");
  }
}
