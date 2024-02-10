using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.Log;

public class CompositeLogger(params ILogger[] _loggers) : ILogger
{
  private readonly ILogger[] p_loggers = _loggers;
  private bool p_disposedValue;

  public void Info(string _text, string? _name = null)
  {
    foreach (ILogger logger in p_loggers)
      logger.Info(_text, _name);
  }

  public void InfoJson<T>(string _text, T _object, string? _name = null) where T : notnull
  {
    foreach (var logger in p_loggers)
      logger.InfoJson(_text, _object, _name);
  }

  public void Warn(string _text, string? _name = null)
  {
    foreach (ILogger logger in p_loggers)
      logger.Warn(_text, _name);
  }

  public void WarnJson<T>(string _text, T _object, string? _scope = null) where T : notnull
  {
    foreach (var logger in p_loggers)
      logger.WarnJson(_text, _object, _scope);
  }

  public void Error(string _text, Exception? _ex = null, string? _name = null)
  {
    foreach (ILogger logger in p_loggers)
      logger.Error(_text, _ex, _name);
  }

  public void ErrorJson<T>(string _text, T _object, string? _scope = null) where T : notnull
  {
    foreach (ILogger logger in p_loggers)
      logger.ErrorJson(_text, _object, _scope);
  }

  public long GetEntriesCount(LogEntryType _type)
  {
    var result = 0L;
    foreach (var logger in p_loggers)
      result += logger.GetEntriesCount(_type);

    return result / p_loggers.Length;
  }

  public ILogger this[string _name] => new NamedLogger(this, _name);

  public void Flush()
  {
    foreach (ILogger logger in p_loggers)
      logger.Flush();
  }

  protected virtual void Dispose(bool _disposing)
  {
    if (!p_disposedValue)
    {
      if (_disposing)
        foreach (var logger in p_loggers)
          logger.Dispose();

      p_disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(_disposing: true);
    GC.SuppressFinalize(this);
  }

}
