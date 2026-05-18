using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Pools;

internal static class SemaphoreSlimPool
{
  private static readonly ConcurrentDictionary<string, SemaphoreSlimWrapper> p_pool = new();

  public static SemaphoreSlimWrapper Get(
    string _key,
    int _capacity)
  {
    return p_pool.GetOrAdd(
      $"{_key}%%%%%{_capacity}",
      _ => new SemaphoreSlimWrapper(new SemaphoreSlim(_capacity, _capacity)));
  }

}

internal class SemaphoreSlimWrapper
{
  private readonly SemaphoreSlim p_semaphore;

  public SemaphoreSlimWrapper(
    SemaphoreSlim _semaphore)
  {
    p_semaphore = _semaphore;
  }

  public Task WaitAsync(CancellationToken _ct) => p_semaphore.WaitAsync(_ct);

  public int Release() => p_semaphore.Release();
}
