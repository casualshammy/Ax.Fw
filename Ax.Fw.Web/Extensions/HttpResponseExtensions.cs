using Ax.Fw.Web.Data.SseServer;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Web.Extensions;

public static class HttpResponseExtensions
{
  /// <summary>
  /// Switch connection to SSE mode.
  /// </summary>
  public static void SetSseHeaders(this HttpResponse _response)
  {
    _response.StatusCode = 200;
    _response.Headers.ContentType = "text/event-stream";
    _response.Headers.CacheControl = "no-cache";
    _response.Headers.Connection = "keep-alive";
    _response.Headers["X-Accel-Buffering"] = "no";
  }

  /// <summary>
  /// Write SSE message to connection.
  /// </summary>
  public static async Task WriteSseMsgAsync(
    this HttpResponse _response,
    SsePreparedMsg _msg,
    CancellationToken _ct)
  {
    string msg;
    if (!_msg.IsComment)
      msg = $"id: {_msg.Id}\ndata: {_msg.JsonData}\n\n";
    else
      msg = $": {_msg.JsonData}\n\n";

    await _response.WriteAsync(msg, _ct);
    await _response.Body.FlushAsync(_ct);
  }

  /// <summary>
  /// Write SSE message.
  /// </summary>
  /// <typeparam name="T">Type of payload.</typeparam>
  /// <param name="_response">HTTP response.</param>
  /// <param name="_id">Id of message.</param>
  /// <param name="_msg">Payload.</param>
  /// <param name="_jsonCtx">JSON serializer context for serialization.</param>
  /// <param name="_ct">Cancellation token.</param>
  public static async Task WriteSseMsgAsync<T>(
    this HttpResponse _response,
    long _id,
    T _msg,
    JsonSerializerContext _jsonCtx,
    CancellationToken _ct)
  {
    var type = typeof(T);
    var json = JsonSerializer.Serialize(_msg, type, _jsonCtx);
    var msg = new SsePreparedMsg(_id, type.Name, json, false);
    await WriteSseMsgAsync(_response, msg, _ct);
  }

  public static async Task WriteSseCommentAsync(
    this HttpResponse _response,
    string _comment,
    CancellationToken _ct)
  {
    var msg = new SsePreparedMsg(0L, "comment", _comment, true);
    await WriteSseMsgAsync(_response, msg, _ct);
  }

}
