using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.Log;

public class NamedLogger : ILogger
{
  private readonly ILogger p_logger;
  private readonly string p_name;

  public NamedLogger(ILogger _logger, string _scope)
  {
    if (string.IsNullOrEmpty(_scope))
      throw new ArgumentException($"'{nameof(_scope)}' cannot be null or empty.", nameof(_scope));

    p_logger = _logger ?? throw new ArgumentNullException(paramName: nameof(_logger));

    if (_logger is not NamedLogger namedLogger)
      p_name = _scope;
    else
      p_name = $"{namedLogger.p_name} | {_scope}";
  }

  public void Info(string _text, string? _overrideScope = null) => p_logger.Info(_text, _overrideScope ?? p_name);

  public void InfoJson<T>(string _text, T _object, string? _overrideScope = null) where T : notnull => p_logger.InfoJson(_text, _object, _overrideScope ?? p_name);

  public void Warn(string _text, string? _overrideScope = null) => p_logger.Warn(_text, _overrideScope ?? p_name);

  public void WarnJson<T>(string _text, T _object, string? _overrideScope = null) where T : notnull => p_logger.WarnJson(_text, _object, _overrideScope ?? p_name);

  public void Error(string _text, Exception? _ex = null, string? _overrideScope = null) => p_logger.Error(_text, _ex, _overrideScope ?? p_name);

  public void ErrorJson<T>(string _text, T _object, string? _overrideScope = null) where T : notnull => p_logger.ErrorJson(_text, _object, _overrideScope ?? p_name);

  public long GetEntriesCount(LogEntryType _type) => p_logger.GetEntriesCount(_type);

  public ILogger this[string _scope] => new NamedLogger(this, _scope);

  public void Flush() => p_logger.Flush();

  public void Dispose() => p_logger.Dispose();

}
