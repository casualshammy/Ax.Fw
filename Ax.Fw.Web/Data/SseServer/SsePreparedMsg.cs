namespace Ax.Fw.Web.Data.SseServer;

public sealed record SsePreparedMsg(
  long Id,
  string Type,
  string JsonData,
  bool IsComment);