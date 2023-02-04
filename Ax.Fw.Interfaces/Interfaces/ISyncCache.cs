using System;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface ISyncCache<TKey, TValue> where TKey : notnull
{
  int Count { get; }

  Task<TValue?> GetOrPutAsync(TKey _key, Func<TKey, Task<TValue?>> _factory);
  Task<TValue?> GetOrPutAsync(TKey _key, Func<TKey, Task<TValue?>> _factory, TimeSpan _overrideTtl);
  TValue? GetOrPut(TKey _key, Func<TKey, TValue?> _factory, TimeSpan _overrideTtl);
  void Put(TKey _key, TValue? _value);
  void Put(TKey _key, TValue? _value, TimeSpan _overrideTtl);
  bool TryGet(TKey _key, out TValue? _value);
  TValue? GetOrPut(TKey _key, Func<TKey, TValue?> _factory);
    System.Collections.Generic.IReadOnlyDictionary<TKey, TValue?> GetValues();
    bool TryRemove(TKey _key, out TValue? _value);
}