using Ax.Fw.SharedTypes.Data.Log;
using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface ILog
{
  ILog this[string _scope] { get; }
  string? Scope { get; }

  void Info(string _text);
  void Warn(string _text);
  void Error(string _text, Exception? _ex = null);
  long GetEntriesCount(LogEntryType _type);

}
