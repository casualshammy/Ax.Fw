namespace Ax.Fw.Web.Data.WsServer;

public sealed record WsBaseMsg(
  string Type, 
  object Payload);
