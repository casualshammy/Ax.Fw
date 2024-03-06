using Ax.Fw.SharedTypes.Data.Log;
using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface ILog
{
  ILog this[string _scope] { get; }
  string? Scope { get; }

  void Info(string _text);
  void InfoJson<T>(string _text, T _object) where T : notnull;
  void Warn(string _text);
  void WarnJson<T>(string _text, T _object) where T : notnull;
  void Error(string _text, Exception? _ex = null);
  void ErrorJson<T>(string _text, T _object) where T : notnull;
  long GetEntriesCount(LogEntryType _type);

}
