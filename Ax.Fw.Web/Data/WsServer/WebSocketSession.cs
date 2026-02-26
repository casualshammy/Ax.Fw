using Ax.Fw.SharedTypes.Interfaces;
using System.Net.WebSockets;

namespace Ax.Fw.Web.Data.WsServer;

public sealed class WebSocketSession<TClientData, TClientGroup>
  where TClientData : notnull, IEquatable<TClientData>
  where TClientGroup : notnull, IEquatable<TClientGroup>
{
  private readonly WebSocket p_webSocket;
  private readonly SemaphoreSlim p_sendSemaphore;

  internal WebSocketSession(
    IReadOnlyLifetime _lifetime,
    Guid _connectionId,
    TClientData _clientData,
    TClientGroup _clientGroup,
    WebSocket _socket)
  {
    p_sendSemaphore = _lifetime.ToDisposeOnEnded(new SemaphoreSlim(1, 1));

    ConnectionId = _connectionId;
    ClientData = _clientData;
    ClientGroup = _clientGroup;
    p_webSocket = _socket;
  }

  public Guid ConnectionId { get; }
  public TClientData ClientData { get; }
  public TClientGroup ClientGroup { get; }
  public WebSocketState SocketState => p_webSocket.State;

  public async Task SendAsync(
    ReadOnlyMemory<byte> _buffer,
    WebSocketMessageType _msgType,
    bool _endOfMessage,
    CancellationToken _ct)
  {
    await p_sendSemaphore.WaitAsync(_ct);

    try
    {
      await p_webSocket.SendAsync(_buffer, _msgType, _endOfMessage, _ct);
    }
    finally
    {
      p_sendSemaphore.Release();
    }
  }

  public Task<WebSocketReceiveResult> ReceiveAsync(
    ArraySegment<byte> _buffer,
    CancellationToken _ct)
    => p_webSocket.ReceiveAsync(_buffer, _ct);

  public Task CloseAsync(WebSocketCloseStatus _closeStatus,
    string? _statusDescription,
    CancellationToken _ct)
    => p_webSocket.CloseAsync(_closeStatus, _statusDescription, _ct);

}