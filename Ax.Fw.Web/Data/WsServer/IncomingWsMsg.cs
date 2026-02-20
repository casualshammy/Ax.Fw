namespace Ax.Fw.Web.Data.WsServer;

/// <summary>
/// Incoming WebSocket message.
/// </summary>
/// <typeparam name="TClientId">Type of identifier of client.</typeparam>
/// <typeparam name="TSessionGroup">Type of identifier of session group.</typeparam>
/// <typeparam name="TData">Type of payload.</typeparam>
/// <param name="ConnectionId">Unique identifier of connection.</param>
/// <param name="ClientId">Identifier of client.<</param>
/// <param name="SessionGroup">Identifier of session group.</param>
/// <param name="Data">Payload.</param>
public record class IncomingWsMsg<TClientId, TSessionGroup, TData>(
  Guid ConnectionId,
  TClientId ClientId,
  TSessionGroup SessionGroup,
  TData Data)
  where TSessionGroup : notnull, IEquatable<TSessionGroup>
  where TClientId : notnull, IEquatable<TClientId>;
