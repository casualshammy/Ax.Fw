#nullable enable
using System;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface ISyncCache<TKey, TValue>
    {
        int Count { get; }

        Task<TValue?> Get(TKey _key, Func<TKey, Task<TValue?>> _factory);
        Task<TValue?> GetOrPut(TKey _key, Func<TKey, Task<TValue?>> _factory, TimeSpan _overrideTtl);
        void Put(TKey _key, TValue? _value);
        void Put(TKey _key, TValue? _value, TimeSpan _overrideTtl);
        bool TryGet(TKey _key, out TValue? _value);
    }
}