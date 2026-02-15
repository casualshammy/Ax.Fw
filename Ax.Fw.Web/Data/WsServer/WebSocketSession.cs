using System.Net.WebSockets;

namespace Ax.Fw.Web.Data.WsServer;

public sealed record WebSocketSession<TSessionId, TSessionGroup>(
  TSessionGroup SessionGroup,
  TSessionId Id,
  WebSocket Socket)
  where TSessionGroup : notnull, IEquatable<TSessionGroup>
  where TSessionId : notnull, IEquatable<TSessionId>;
