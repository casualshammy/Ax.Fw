using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IJsonStorage<T>
{
  string JsonFilePath { get; }

  T Read(Func<T> _defaultFactory);
  Task<T> ReadAsync(Func<CancellationToken, Task<T>> _defaultFactory, CancellationToken _ct);
  Task WriteAsync(T? _data, CancellationToken _ct);
  void Write(T? _data);
}