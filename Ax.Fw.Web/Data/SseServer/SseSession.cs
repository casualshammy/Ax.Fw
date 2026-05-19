using Microsoft.AspNetCore.Http;
using System.Threading.Channels;

namespace Ax.Fw.Web.Data.SseServer;

public sealed class SseSession<TClientData, TClientGroup>
  where TClientData : notnull, IEquatable<TClientData>
  where TClientGroup : notnull, IEquatable<TClientGroup>
{
  private static long p_msgCounter = -1;
  private readonly Channel<SseBaseIdMsg> p_channel;

  internal SseSession(
    Guid _connectionId,
    TClientData _clientData,
    TClientGroup _clientGroup,
    int _queueSize)
  {
    ConnectionId = _connectionId;
    ClientData = _clientData;
    ClientGroup = _clientGroup;

    p_channel = Channel.CreateBounded<SseBaseIdMsg>(new BoundedChannelOptions(_queueSize)
    {
      FullMode = BoundedChannelFullMode.DropOldest,
    });
  }

  public Guid ConnectionId { get; }
  public TClientData ClientData { get; }
  public TClientGroup ClientGroup { get; }

  internal void Write(SseBaseMsg _msg)
    => p_channel.Writer.TryWrite(new SseBaseIdMsg(Interlocked.Increment(ref p_msgCounter), _msg.Type, _msg.JsonData));

  /// <summary>
  /// Gets the channel reader used to receive server-sent event messages.
  /// </summary>
  /// <returns>A <see cref="ChannelReader{SseBaseIdMsg}"/> that provides asynchronous access to incoming server-sent event
  /// messages.</returns>
  public ChannelReader<SseBaseIdMsg> GetReader()
    => p_channel.Reader;

  /// <summary>
  /// Asynchronously reads server-sent event messages from the channel, optionally filtering messages based on the
  /// Last-Event-ID header in the HTTP request.
  /// </summary>
  /// <remarks>If the Last-Event-ID header is not present or cannot be parsed as an integer, all messages are
  /// returned. This method is typically used to resume message streams from a specific event ID in server-sent events
  /// scenarios.</remarks>
  /// <param name="_httpRequest">The HTTP request.</param>
  /// <param name="_ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
  /// <returns>An asynchronous stream of <see cref="SseBaseIdMsg"/> messages.</returns>
  public IAsyncEnumerable<SseBaseIdMsg> ReadMessagesAsync(
    HttpRequest _httpRequest,
    CancellationToken _ct)
  {
    long? lastEventId = null;
    if (_httpRequest.Headers.TryGetValue("Last-Event-ID", out var rawLastEventId))
      if (long.TryParse(rawLastEventId, out var lastId))
        lastEventId = lastId;

    if (lastEventId == null)
      return p_channel.Reader.ReadAllAsync(_ct);
    else
      return p_channel.Reader.ReadAllAsync(_ct)
        .Where(_ => _.Id > lastEventId.Value);
  }

}