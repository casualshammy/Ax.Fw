using System;

namespace Ax.Fw.SharedTypes.Data.Log;

public record LogEntry(LogEntryType Type, string Text, DateTimeOffset Time, string? Scope)
{
  public string GetTypePrefix() => Type switch
  {
    LogEntryType.WARN => "W",
    LogEntryType.ERROR => "E",
    _ => " ",
  };
}
