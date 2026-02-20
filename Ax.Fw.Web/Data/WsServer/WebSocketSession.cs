using System.Net.WebSockets;

namespace Ax.Fw.Web.Data.WsServer;

public sealed record WebSocketSession<TClientId, TSessionGroup>(
  Guid ConnectionId,
  TSessionGroup SessionGroup,
  TClientId SessionId,
  WebSocket Socket)
  where TSessionGroup : notnull, IEquatable<TSessionGroup>
  where TClientId : notnull, IEquatable<TClientId>;
