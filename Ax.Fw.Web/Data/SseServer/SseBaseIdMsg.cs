namespace Ax.Fw.Web.Data.SseServer;

public record SseBaseIdMsg(long Id, string Type, string JsonData);