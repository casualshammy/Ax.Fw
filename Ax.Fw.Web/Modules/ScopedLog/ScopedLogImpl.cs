using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Web.Interfaces;

namespace Ax.Fw.Web.Modules.ScopedLog;

internal class ScopedLogImpl : IScopedLog
{
  private readonly ILog p_log;

  public ScopedLogImpl(ILog _log)
  {
    p_log = _log;
  }

  public string? Scope => p_log.Scope;

  public ILog this[string _scope] => p_log[_scope];

  public void Info(string _message) => p_log.Info(_message);
  public void Warn(string _message) => p_log.Warn(_message);
  public void Error(string _message) => p_log.Error(_message);
  public void Error(string _text, Exception? _ex = null) => p_log.Error(_text, _ex);

  public long GetEntriesCount(LogEntryType _type) => p_log.GetEntriesCount(_type);
}