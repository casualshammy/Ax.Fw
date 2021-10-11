#nullable enable
using Ax.Fw.Internals;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Cache
{
    public class SyncCache<TKey, TValue>
    {
        public SyncCache(SyncCacheSettings _settings)
        {
            p_settings = _settings;
        }

        public int Count => p_table.Count;

        private readonly ConcurrentDictionary<TKey, SyncCacheEntry<TValue?>> p_table = new();
        private readonly SyncCacheSettings p_settings;
        private readonly object p_addRemoveLock = new();
        private long p_sharedIndex = 0;

        public bool TryGet(TKey _key, out TValue? _value)
        {
            var now = DateTimeOffset.UtcNow;
            if (p_table.TryGetValue(_key, out SyncCacheEntry<TValue?> entry))
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

        public async Task<TValue?> Get(TKey _key, Func<TKey, Task<TValue?>> _factory, TimeSpan _overrideTtl)
        {
            if (!TryGet(_key, out var value))
            {
                value = await _factory(_key);
                Put(_key, value, _overrideTtl);
            }
            return value;
        }

        public async Task<TValue?> Get(TKey _key, Func<TKey, Task<TValue?>> _factory)
        {
            return await Get(_key, _factory, p_settings.TTL);
        }

        public void Put(TKey _key, TValue? _value, TimeSpan _overrideTtl)
        {
            var now = DateTimeOffset.UtcNow;

            lock (p_addRemoveLock)
            {
                var newEntry = new SyncCacheEntry<TValue?>(now + _overrideTtl, _value, Interlocked.Increment(ref p_sharedIndex));
                p_table.AddOrUpdate(_key, newEntry, (_x, _y) => newEntry);
            }

            int capacity = p_settings.Capacity;
            int overhead = p_settings.Overhead;

            if (p_table.Count > capacity + overhead)
                lock (p_addRemoveLock)
                    if (p_table.Count > capacity + overhead)
                    {
                        var list = p_table.ToList();
                        list.Sort((a, b) => a.Value.ValidUntil.CompareTo(b.Value.ValidUntil));
                        int howManyToDelete = p_table.Count - capacity;
                        for (int i = 0; i < howManyToDelete; i++)
                            if (!p_table.TryRemove(list[i].Key, out _))
                                throw new Exception($"{nameof(SyncCache<TKey, TValue>)}.{nameof(Put)}(): can't remove value from dictionary");
                    }
        }

        public void Put(TKey _key, TValue? _value)
        {
            Put(_key, _value, p_settings.TTL);
        }

    }
}
