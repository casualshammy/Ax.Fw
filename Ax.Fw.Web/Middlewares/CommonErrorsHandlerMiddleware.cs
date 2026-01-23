using Ax.Fw.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Ax.Fw.Web.Middlewares;

public class CommonErrorsHandlerMiddleware : IMiddleware
{
  private readonly IScopedLog p_log;

  public CommonErrorsHandlerMiddleware(
    IScopedLog _log)
  {
    p_log = _log;
  }

  public async Task InvokeAsync(HttpContext _ctx, RequestDelegate _next)
  {
    try
    {
      await _next(_ctx);
    }
    catch (AccessViolationException avEx)
    {
      p_log.Warn($"Operation is forbidden: {avEx}");
      await Results.Problem(detail: avEx.Message, statusCode: (int)HttpStatusCode.Forbidden).ExecuteAsync(_ctx);
    }
    catch (BadHttpRequestException bhEx)
    {
      p_log.Warn($"Bad request: {bhEx}");
      await Results.Problem(detail: bhEx.Message, statusCode: (int)HttpStatusCode.BadRequest).ExecuteAsync(_ctx);
    }
    catch (Exception ex)
    {
      p_log.Error($"Request is failed: {ex}");
      await Results.Problem(detail: ex.Message, statusCode: (int)HttpStatusCode.InternalServerError).ExecuteAsync(_ctx);
    }
  }
}
