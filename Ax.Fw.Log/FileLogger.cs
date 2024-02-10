using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Timers;

namespace Ax.Fw.Log;

public class FileLogger : ILogger
{
  private readonly ConcurrentQueue<LogEntry> p_buffer = new();
  private readonly Func<string?> p_filename;
  private readonly ConcurrentDictionary<LogEntryType, long> p_stats = new();
  private readonly Action<Exception, IEnumerable<LogEntry>>? p_onErrorHandler;
  private readonly Func<LogEntry, string> p_logEntryFormat;
  private readonly HashSet<string> p_filesWrote = [];
  private readonly System.Timers.Timer p_timer;
  private readonly JsonSerializerOptions p_jsonSerializerOptions;
  private bool p_disposedValue;

  public FileLogger(
    Func<string?> _filenameFactory,
    TimeSpan _buffer,
    Func<LogEntry, string>? _logEntryFormat = null,
    Action<Exception, IEnumerable<LogEntry>>? _onError = null)
  {
    ArgumentNullException.ThrowIfNull(_filenameFactory);

    p_onErrorHandler = _onError;
    p_filename = _filenameFactory;

    if (_logEntryFormat == null)
      p_logEntryFormat = _logEntry =>
      {
        if (_logEntry.LogName != null)
          return $"| {_logEntry.GetTypePrefix()} | {_logEntry.Time:dd.MM.yyyy HH:mm:ss.fff} || {_logEntry.LogName} || {_logEntry.Text}";
        else
          return $"| {_logEntry.GetTypePrefix()} | {_logEntry.Time:dd.MM.yyyy HH:mm:ss.fff} || {_logEntry.Text}";
      };
    else
      p_logEntryFormat = _logEntryFormat;

    p_timer = new System.Timers.Timer(_buffer.TotalMilliseconds);
    p_timer.Elapsed += Timer_Elapsed;
    p_timer.Start();

    p_jsonSerializerOptions = new JsonSerializerOptions
    {
      WriteIndented = true,
    };
  }

  public IReadOnlySet<string> LogFilesWrote => p_filesWrote;

  public TimeSpan Buffer
  {
    get => TimeSpan.FromMilliseconds(p_timer.Interval);
    set => p_timer.Interval = value.TotalMilliseconds;
  }

  public FileInfo? CurrentLogFile
  {
    get
    {
      var filePath = p_filename();
      if (filePath == null)
        return null;

      return new(filePath);
    }
  }

  public void Info(string _text, string? _scope = null)
  {
    p_stats.AddOrUpdate(LogEntryType.INFO, 1, (_, _prevValue) => ++_prevValue);
    p_buffer.Enqueue(new LogEntry(LogEntryType.INFO, _text, DateTimeOffset.UtcNow, _scope));
  }

  public void InfoJson<T>(string _text, T _object, string? _scope = null) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.INFO, 1, (_, _prevValue) => ++_prevValue);

    var entry = new LogEntry(
      LogEntryType.INFO,
      $"{_text}{Environment.NewLine}{JsonSerializer.Serialize(_object, p_jsonSerializerOptions)}",
      DateTimeOffset.UtcNow,
      _scope);

    p_buffer.Enqueue(entry);
  }

  public void Warn(string _text, string? _scope = null)
  {
    p_stats.AddOrUpdate(LogEntryType.WARN, 1, (_, _prevValue) => ++_prevValue);
    p_buffer.Enqueue(new LogEntry(LogEntryType.WARN, _text, DateTimeOffset.UtcNow, _scope));
  }

  public void WarnJson<T>(string _text, T _object, string? _scope = null) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.WARN, 1, (_, _prevValue) => ++_prevValue);

    var entry = new LogEntry(
      LogEntryType.WARN,
      $"{_text}{Environment.NewLine}{JsonSerializer.Serialize(_object, p_jsonSerializerOptions)}",
      DateTimeOffset.UtcNow,
      _scope);

    p_buffer.Enqueue(entry);
  }

  public void Error(string _text, Exception? _ex = null, string? _scope = null)
  {
    p_stats.AddOrUpdate(LogEntryType.ERROR, 1, (_, _prevValue) => ++_prevValue);

    if (_ex == null)
      p_buffer.Enqueue(new LogEntry(LogEntryType.ERROR, _text, DateTimeOffset.UtcNow, _scope));
    else
      p_buffer.Enqueue(new LogEntry(LogEntryType.ERROR, $"{_text}\n({_ex.GetType()}) {_ex.Message}\n{new StackTrace(_ex, 1, true)}", DateTimeOffset.UtcNow, _scope));
  }

  public void ErrorJson<T>(string _text, T _object, string? _scope = null) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.ERROR, 1, (_, _prevValue) => ++_prevValue);

    var entry = new LogEntry(
      LogEntryType.ERROR,
      $"{_text}{Environment.NewLine}{JsonSerializer.Serialize(_object, p_jsonSerializerOptions)}",
      DateTimeOffset.UtcNow,
      _scope);

    p_buffer.Enqueue(entry);
  }

  public long GetEntriesCount(LogEntryType _type)
  {
    p_stats.TryGetValue(_type, out long value);
    return value;
  }

  public ILogger this[string _scope] => new NamedLogger(this, _scope);

  protected virtual void Dispose(bool _disposing)
  {
    if (!p_disposedValue)
    {
      if (_disposing)
      {
        p_timer.Elapsed -= Timer_Elapsed;
        p_timer.Dispose();
        Flush();
      }
      p_disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(_disposing: true);
    GC.SuppressFinalize(this);
  }

  public void Flush()
  {
    try
    {
      if (!p_buffer.IsEmpty)
      {
        var filepath = p_filename();
        if (filepath == null)
          return;

        var stringBuilder = new StringBuilder();

        while (p_buffer.TryDequeue(out var logEntry))
          stringBuilder.AppendLine(p_logEntryFormat(logEntry));

        p_filesWrote.Add(filepath);
        File.AppendAllText(filepath, stringBuilder.ToString(), Encoding.UTF8);

        stringBuilder.Clear();
      }
    }
    catch (Exception ex)
    {
      p_onErrorHandler?.Invoke(ex, p_buffer.Select(_ => _));
    }
  }

  private void Timer_Elapsed(object? _, ElapsedEventArgs __) => Flush();

}
