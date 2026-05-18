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

  public void Write(SseBaseMsg _msg)
    => p_channel.Writer.TryWrite(new SseBaseIdMsg(Interlocked.Increment(ref p_msgCounter), _msg.Type, _msg.JsonData));

  public ChannelReader<SseBaseIdMsg> GetReader()
    => p_channel.Reader;

}