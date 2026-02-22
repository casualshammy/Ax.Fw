namespace Ax.Fw.Web.Data.WsServer;

/// <summary>
/// Incoming WebSocket message.
/// </summary>
/// <typeparam name="TClientData">The type used to store some client-specific data.</typeparam>
/// <typeparam name="TClientGroup">The type used to group WebSocket clients.</typeparam>
/// <typeparam name="TData">Type of payload.</typeparam>
/// <param name="ConnectionId">Unique identifier of connection.</param>
/// <param name="ClientData">Data of client.<</param>
/// <param name="ClientGroup">Group of client.</param>
/// <param name="Data">Payload.</param>
public record class IncomingWsMsg<TClientData, TClientGroup, TData>(
  Guid ConnectionId,
  TClientData ClientData,
  TClientGroup ClientGroup,
  TData Data)
  where TClientData : notnull, IEquatable<TClientData>
  where TClientGroup : notnull, IEquatable<TClientGroup>;
