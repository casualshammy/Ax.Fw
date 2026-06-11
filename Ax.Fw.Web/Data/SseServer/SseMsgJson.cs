namespace Ax.Fw.Web.Data.SseServer;

internal sealed record SseMsgJson(
  string MsgType,
  string JsonData,
  bool IsComment = false);
