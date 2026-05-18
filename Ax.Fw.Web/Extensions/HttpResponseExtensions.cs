using Ax.Fw.Web.Data.SseServer;
using Microsoft.AspNetCore.Http;

namespace Ax.Fw.Web.Extensions;

public static class HttpResponseExtensions
{
  public static void SetSseHeaders(this HttpResponse _response)
  {
    _response.StatusCode = 200;
    _response.Headers.ContentType = "text/event-stream";
    _response.Headers.CacheControl = "no-cache";
    _response.Headers.Connection = "keep-alive";
  }

  public static async Task WriteSseMsgAsync(
    this HttpResponse _response,
    SseBaseIdMsg _msg,
    CancellationToken _ct)
  {
    var msg = $"id: {_msg.Id}\nevent: {_msg.Type}\ndata: {_msg.JsonData}\n\n";
    await _response.WriteAsync(msg, _ct);
    await _response.Body.FlushAsync(_ct);
  }
}
