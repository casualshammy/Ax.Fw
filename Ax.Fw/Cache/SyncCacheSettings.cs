using System;

namespace Ax.Fw.Cache
{
    public class SyncCacheSettings
    {
        public SyncCacheSettings() { }

        public SyncCacheSettings(int _capacity, int _overhead, TimeSpan _ttl)
        {
            Capacity = _capacity;
            Overhead = _overhead;
            TTL = _ttl;
        }

        public int Capacity { get; private set; } = 1000;
        public int Overhead { get; private set; } = 100;
        public TimeSpan TTL { get; private set; } = TimeSpan.FromDays(1);

        public SyncCacheSettings Validate()
        {
            if (Capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(Capacity), $"{nameof(Capacity)} must be bigger than 0");
            if (Overhead <= 0)
                throw new ArgumentOutOfRangeException(nameof(Overhead), $"{nameof(Overhead)} must be bigger than 0");
            if (TTL.TotalMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(TTL), $"{nameof(TTL)} must be bigger than 0ms");
            return this;
        }

    }
}
