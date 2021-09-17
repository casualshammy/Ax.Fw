using System;

namespace Ax.Fw.Internals
{
    internal class SyncCacheEntry<TValue>
    {
        public SyncCacheEntry(DateTimeOffset _validUntil, TValue _data, long _index)
        {
            ValidUntil = _validUntil;
            Data = _data;
        }

        public DateTimeOffset ValidUntil { get; set; }
        public TValue Data { get; set; }
    }
}
