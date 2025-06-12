using Ax.Fw.Cache.Parts;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Ax.Fw.Cache;

public class SyncCache<TKey, TValue> : ISyncCache<TKey, TValue> where TKey : notnull
{
  private readonly ConcurrentDictionary<TKey, SyncCacheEntry<TValue?>> p_table = new();
  private readonly SyncCacheSettings p_settings;
  private readonly object p_addRemoveLock = new();

  public SyncCache(SyncCacheSettings _settings)
  {
    p_settings = _settings;
  }

  public int Count => p_table.Count;

  public IReadOnlyDictionary<TKey, TValue?> GetValues()
  {
    var now = DateTimeOffset.UtcNow;
    var result = p_table
      .Where(_x => _x.Value.ValidUntil > now)
      .ToImmutableDictionary(_x => _x.Key, _x => _x.Value.Data);

    return result;
  }

  public bool TryGet(TKey _key, out TValue? _value)
  {
    var now = DateTimeOffset.UtcNow;
    if (p_table.TryGetValue(_key, out SyncCacheEntry<TValue?>? entry))
    {
      if (entry.ValidUntil > now)
      {
        _value = entry.Data;
        return true;
      }
    }
    _value = default;
    return false;
  }

  public async Task<TValue?> GetOrPutAsync(TKey _key, Func<TKey, Task<TValue?>> _factory, TimeSpan _overrideTtl)
  {
    if (!TryGet(_key, out var value))
    {
      value = await _factory(_key);
      Put(_key, value, _overrideTtl);
    }
    return value;
  }

  public async Task<TValue?> GetOrPutAsync(TKey _key, Func<TKey, Task<TValue?>> _factory) => await GetOrPutAsync(_key, _factory, p_settings.TTL);

  public TValue? GetOrPut(TKey _key, Func<TKey, TValue?> _factory, TimeSpan _overrideTtl)
  {
    if (!TryGet(_key, out var value))
    {
      value = _factory(_key);
      Put(_key, value, _overrideTtl);
    }
    return value;
  }

  public TValue? GetOrPut(TKey _key, Func<TKey, TValue?> _factory) => GetOrPut(_key, _factory, p_settings.TTL);

  public void Put(TKey _key, TValue? _value, TimeSpan _overrideTtl)
  {
    var now = DateTimeOffset.UtcNow;

    lock (p_addRemoveLock)
    {
      var newEntry = new SyncCacheEntry<TValue?>(now + _overrideTtl, _value);
      p_table.AddOrUpdate(_key, newEntry, (_x, _y) => newEntry);
    }

    int capacity = p_settings.Capacity;
    int overhead = p_settings.Overhead;

    if (p_table.Count > capacity + overhead)
      lock (p_addRemoveLock)
        if (p_table.Count > capacity + overhead)
        {
          var counter = 0;
          foreach (var pair in p_table.OrderByDescending(_ => _.Value.ValidUntil))
          {
            if (++counter > capacity)
              if (!p_table.TryRemove(pair.Key, out _))
                throw new Exception($"{nameof(SyncCache<TKey, TValue>)}.{nameof(Put)}(): can't remove value from dictionary");
          }
        }
  }

  public void Put(TKey _key, TValue? _value)
  {
    Put(_key, _value, p_settings.TTL);
  }

  public bool TryRemove(TKey _key, out TValue? _value)
  {
    var now = DateTimeOffset.UtcNow;
    lock (p_addRemoveLock)
    {
      if (p_table.TryRemove(_key, out var cacheEntry) && cacheEntry.ValidUntil >= now)
      {
        _value = cacheEntry.Data;
        return true;
      }
    }

    _value = default;
    return false;
  }

}
