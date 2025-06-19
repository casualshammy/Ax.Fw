using Ax.Fw.Pools;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Cache;

public class AsyncCachedValue<T> : DisposableStack
{
  private static readonly Pool<SemaphoreSlim> p_writeSemaphorePool = new(() => new SemaphoreSlim(1, 1), _s =>
  {
    try
    {
      _s.Release();
    }
    catch (SemaphoreFullException)
    { }
  });
  private readonly long p_ttlMs;
  private readonly Func<CancellationToken, Task<T?>> p_factory;
  private readonly SemaphoreSlim p_writeSemaphore;
  private T? p_value;
  private long p_updated;

  public AsyncCachedValue(TimeSpan _ttl, Func<CancellationToken, Task<T?>> _factory)
  {
    ToDispose(p_writeSemaphorePool.Get(out p_writeSemaphore));
    p_ttlMs = (long)_ttl.TotalMilliseconds;
    p_value = default;
    p_updated = int.MinValue;
    p_factory = _factory;
  }

  public async Task<T?> GetValueAsync(CancellationToken _ct)
  {
    var now = Environment.TickCount64;
    if (now - p_updated < p_ttlMs)
      return p_value;

    await p_writeSemaphore.WaitAsync(_ct);
    try
    {
      now = Environment.TickCount64;
      if (now - p_updated < p_ttlMs)
        return p_value;

      p_value = await p_factory(_ct);
      p_updated = now;
      return p_value;
    }
    finally
    {
      try
      {
        p_writeSemaphore.Release();
      }
      catch (SemaphoreFullException)
      { }
    }
  }

}
