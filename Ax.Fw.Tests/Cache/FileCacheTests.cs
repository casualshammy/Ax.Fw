using Ax.Fw.Cache;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests.Cache;

public class FileCacheTests
{
  private static SemaphoreSlim p_semaphore = new(1, 1);

  [Fact(Timeout = 30000)]
  public async Task NotTimeoutTestAsync()
  {
    await p_semaphore.WaitAsync();
    try
    {
      using var lifetime = new Lifetime();
      var tempDir = Path.Combine(Path.GetTempPath(), Random.Shared.Next().ToString());
      var cache = new FileCache(lifetime, tempDir, TimeSpan.FromMinutes(10), 1024 * 1024, TimeSpan.FromMinutes(10));

      var key = "test-key";
      var data = new byte[1024];
      Random.Shared.NextBytes(data);

      using var ms = new MemoryStream();
      await ms.WriteAsync(data, lifetime.Token);
      ms.Position = 0;

      await cache.StoreAsync(key, ms, true, lifetime.Token);

      using var result = cache.Get(key);

      Assert.NotNull(result);
    }
    finally
    {
      p_semaphore.Release();
    }
  }

  [Fact(Timeout = 30000)]
  public async Task TimeoutTestAsync()
  {
    await p_semaphore.WaitAsync();
    try
    {
      using var lifetime = new Lifetime();
      var tempDir = Path.Combine(Path.GetTempPath(), Random.Shared.Next().ToString());
      var cache = new FileCache(lifetime, tempDir, TimeSpan.FromMilliseconds(500), 1024 * 1024, TimeSpan.FromMinutes(10));

      var key = "test-key";
      var data = new byte[1024];
      Random.Shared.NextBytes(data);

      using var ms = new MemoryStream();
      await ms.WriteAsync(data, lifetime.Token);
      ms.Position = 0;

      await cache.StoreAsync(key, ms, true, lifetime.Token);

      await Task.Delay(TimeSpan.FromMilliseconds(1000));

      using var result = cache.Get(key);

      Assert.Null(result);
    }
    finally
    {
      p_semaphore.Release();
    }
  }

  [Fact(Timeout = 30000)]
  public async Task HitSizeLimitAsync()
  {
    await p_semaphore.WaitAsync();
    try
    {
      using var lifetime = new Lifetime();
      var tempDir = Path.Combine(Path.GetTempPath(), Random.Shared.Next().ToString());
      var cache = new FileCache(lifetime, tempDir, TimeSpan.FromDays(1), 3 * 1024, TimeSpan.FromMinutes(10));

      var data = new byte[1024];
      Random.Shared.NextBytes(data);
      using var ms = new MemoryStream();
      await ms.WriteAsync(data, lifetime.Token);

      for (var i = 0; i < 4; i++)
      {
        var key = $"test-key-{i}";
        ms.Position = 0;
        await cache.StoreAsync(key, ms, true, lifetime.Token);
      }

      await cache.CleanFilesWaitAsync(lifetime.Token);

      Assert.Null(cache.Get($"test-key-0"));
      Assert.NotNull(cache.Get($"test-key-1"));
      Assert.NotNull(cache.Get($"test-key-2"));
      Assert.NotNull(cache.Get($"test-key-3"));
    }
    finally
    {
      p_semaphore.Release();
    }
  }

}
