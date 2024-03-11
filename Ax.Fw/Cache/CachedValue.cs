using Ax.Fw.Pools;
using System;
using System.Threading;

namespace Ax.Fw.Cache;

public class CachedValue<T> : DisposableStack
{
  private static readonly Pool<SemaphoreSlim> p_writeSemaphorePool = new(() => new SemaphoreSlim(1, 1), null);
  private readonly long p_ttlMs;
  private readonly Func<T?> p_factory;
  private readonly SemaphoreSlim p_writeSemaphore;
  private T? p_value;
  private long p_updated;

  public CachedValue(TimeSpan _ttl, Func<T?> _factory)
  {
    ToDispose(p_writeSemaphorePool.Get(out p_writeSemaphore));
    p_ttlMs = (long)_ttl.TotalMilliseconds;
    p_value = default;
    p_updated = int.MinValue;
    p_factory = _factory;
  }

  public T? GetValue()
  {
    var now = Environment.TickCount64;
    if (now - p_updated < p_ttlMs)
      return p_value;

    p_writeSemaphore.Wait();
    try
    {
      now = Environment.TickCount64;
      if (now - p_updated < p_ttlMs)
        return p_value;

      p_value = p_factory();
      p_updated = now;
      return p_value;
    }
    finally
    {
      p_writeSemaphore.Release();
    }
  }

}
