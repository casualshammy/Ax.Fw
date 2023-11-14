using System;

namespace Ax.Fw.Cache;

public class SyncCacheSettings
{
  public SyncCacheSettings() : this(1000, 100, TimeSpan.FromDays(1)) { }

  public SyncCacheSettings(int _capacity, int _overhead, TimeSpan _ttl)
  {
    if (_capacity <= 0)
      throw new ArgumentOutOfRangeException(nameof(Capacity), $"{nameof(Capacity)} must be bigger than 0");
    if (_overhead <= 0)
      throw new ArgumentOutOfRangeException(nameof(Overhead), $"{nameof(Overhead)} must be bigger than 0");
    if (_ttl.TotalMilliseconds <= 0)
      throw new ArgumentOutOfRangeException(nameof(TTL), $"{nameof(TTL)} must be bigger than 0ms");

    Capacity = _capacity;
    Overhead = _overhead;
    TTL = _ttl;
  }

  public int Capacity { get; }
  public int Overhead { get; }
  public TimeSpan TTL { get; }

}
