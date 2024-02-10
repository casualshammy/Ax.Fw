using System;

namespace Ax.Fw.SharedTypes.Data.Log;

public class LogEntry
{
  public LogEntryType Type;
  public string Text;
  public DateTimeOffset Time;
  public string? LogName;

  public LogEntry(LogEntryType _type, string _text, DateTimeOffset _time, string? _scope)
  {
    Type = _type;
    Text = _text;
    Time = _time;
    LogName = _scope;
  }

  public char GetTypePrefix() => Type switch
  {
    LogEntryType.WARN => 'W',
    LogEntryType.ERROR => 'E',
    _ => ' ',
  };

}
