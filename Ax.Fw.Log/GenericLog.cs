using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Log;

public class GenericLog : DisposableStack, ILog
{
  private readonly ReplaySubject<LogEntry> p_logEntriesSubj;
  private readonly ConcurrentDictionary<LogEntryType, long> p_stats;
  private readonly ConcurrentStack<Action> p_endActions;
  private readonly string p_scopeSeparator;
  private JsonSerializerContext? p_jsonCtx;

  public GenericLog(JsonSerializerContext? _jsonCtx, string _scopeSeparator = "/")
  {
    p_jsonCtx = _jsonCtx;
    p_scopeSeparator = _scopeSeparator;
    p_stats = new();
    p_endActions = new();

    p_logEntriesSubj = ToDisposeOnEnded(new ReplaySubject<LogEntry>(100));
    LogEntries = p_logEntriesSubj;

    ToDoOnDisposing(() =>
    {
      while (p_endActions.TryPop(out var action))
        action();
    });

    Scope = "";
  }

  internal GenericLog(GenericLog _parentLog, string _scope)
  {
    p_jsonCtx = _parentLog.p_jsonCtx;
    p_scopeSeparator = _parentLog.p_scopeSeparator;
    p_stats = _parentLog.p_stats;
    p_endActions = _parentLog.p_endActions;

    p_logEntriesSubj = _parentLog.p_logEntriesSubj;
    LogEntries = p_logEntriesSubj;

    if (_parentLog.Scope.IsNullOrEmpty())
      Scope = _scope;
    else
      Scope = $"{_parentLog.Scope}{p_scopeSeparator}{_scope}";
  }

  public ILog this[string _scope] => new GenericLog(this, _scope);
  public string Scope { get; }
  public IObservable<LogEntry> LogEntries { get; }

  public void Info(string _text)
  {
    var logEntry = new LogEntry(LogEntryType.INFO, _text, DateTimeOffset.UtcNow, Scope);
    OnNewLogEntry(logEntry);
  }

  public void InfoJson<T>(string _text, T _object) where T : notnull
  {
    string json;
    if (p_jsonCtx != null)
      json = JsonSerializer.Serialize(_object, typeof(T), p_jsonCtx);
    else
      json = JsonSerializer.Serialize(_object);

    Info($"{_text}{Environment.NewLine}{json}");
  }

  public void Warn(string _text)
  {
    var logEntry = new LogEntry(LogEntryType.WARN, _text, DateTimeOffset.UtcNow, Scope);
    OnNewLogEntry(logEntry);
  }

  public void WarnJson<T>(string _text, T _object) where T : notnull
  {
    string json;
    if (p_jsonCtx != null)
      json = JsonSerializer.Serialize(_object, typeof(T), p_jsonCtx);
    else
      json = JsonSerializer.Serialize(_object);

    Warn($"{_text}{Environment.NewLine}{json}");
  }

  public void Error(string _text, Exception? _ex = null)
  {
    LogEntry logEntry;
    if (_ex != null)
      logEntry = new LogEntry(LogEntryType.ERROR, $"{_text}{Environment.NewLine}{_ex}", DateTimeOffset.UtcNow, Scope);
    else
      logEntry = new LogEntry(LogEntryType.ERROR, $"{_text}", DateTimeOffset.UtcNow, Scope);

    OnNewLogEntry(logEntry);
  }

  public void ErrorJson<T>(string _text, T _object) where T : notnull
  {
    string json;
    if (p_jsonCtx != null)
      json = JsonSerializer.Serialize(_object, typeof(T), p_jsonCtx);
    else
      json = JsonSerializer.Serialize(_object);

    Error($"{_text}{Environment.NewLine}{json}");
  }

  public long GetEntriesCount(LogEntryType _type) => p_stats.GetValueOrDefault(_type);

  public void SetJsonCtx(JsonSerializerContext? _jsonCtx) => p_jsonCtx = _jsonCtx;

  public void AddEndAction(Action _action) => p_endActions.Push(_action);

  internal void OnNewLogEntry(LogEntry _entry)
  {
    p_stats.AddOrUpdate(_entry.Type, 1, (_, _prevValue) => ++_prevValue);
    p_logEntriesSubj.OnNext(_entry);
  }

}
