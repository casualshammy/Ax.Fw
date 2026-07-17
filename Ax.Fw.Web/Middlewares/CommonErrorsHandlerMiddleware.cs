using Ax.Fw.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Ax.Fw.Web.Middlewares;

/// <summary>
/// Middleware for centralized handling of common HTTP request exceptions.
/// </summary>
/// <remarks>
/// This middleware catches common exception types and converts them into appropriate
/// HTTP problem details responses (RFC 7807). It handles the following exception types:
/// <list type="bullet">
///   <item><description><see cref="AccessViolationException"/> → 403 Forbidden</description></item>
///   <item><description><see cref="BadHttpRequestException"/> → 400 Bad Request</description></item>
///   <item><description><see cref="OperationCanceledException"/> → Request cancelled (no response body)</description></item>
///   <item><description>Any other <see cref="Exception"/> → 500 Internal Server Error</description></item>
/// </list>
/// All exceptions are logged via <see cref="IScopedLog"/> at appropriate log levels.
/// </remarks>
public class CommonErrorsHandlerMiddleware : IMiddleware
{
  private readonly IScopedLog p_log;

  /// <summary>
  /// Initializes a new instance of the <see cref="CommonErrorsHandlerMiddleware"/> class.
  /// </summary>
  /// <param name="_log">The scoped logger instance for structured logging.</param>
  public CommonErrorsHandlerMiddleware(
    IScopedLog _log)
  {
    p_log = _log;
  }

  /// <summary>
  /// Invokes the middleware to handle the HTTP request and catch exceptions.
  /// </summary>
  /// <param name="_ctx">The HTTP context for the current request.</param>
  /// <param name="_next">The delegate representing the next middleware in the pipeline.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
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
    catch (OperationCanceledException)
    {
      p_log.Warn($"Request is canceled by client");
    }
    catch (Exception ex)
    {
      p_log.Error($"Request is failed: {ex}");
      await Results.Problem(detail: ex.Message, statusCode: (int)HttpStatusCode.InternalServerError).ExecuteAsync(_ctx);
    }
  }
}
