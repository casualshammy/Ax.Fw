using Ax.Fw.Cache;
using Ax.Fw.Extensions;
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

      await cache.StoreAsync(key, ms, lifetime.Token);

      using var result = await cache.GetAsync(key, lifetime.Token);

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

      await cache.StoreAsync(key, ms, lifetime.Token);

      await Task.Delay(TimeSpan.FromMilliseconds(1000));

      using var result = await cache.GetAsync(key, lifetime.Token);

      Assert.Null(result);
    }
    finally
    {
      p_semaphore.Release();
    }
  }

  [Fact(Timeout = 30000)]
  public async Task AutoCleanUpTextAsync()
  {
    await p_semaphore.WaitAsync();
    try
    {
      using var lifetime = new Lifetime();
      var tempDir = Path.Combine(Path.GetTempPath(), Random.Shared.Next().ToString());
      var cache = new FileCache(lifetime, tempDir, TimeSpan.FromMilliseconds(500), 1024 * 1024, TimeSpan.FromMilliseconds(100));

      var key = "test-key";
      var data = new byte[1024];
      Random.Shared.NextBytes(data);

      using var ms = new MemoryStream();
      await ms.WriteAsync(data, lifetime.Token);
      ms.Position = 0;

      await cache.StoreAsync(key, ms, lifetime.Token);
      await Task.Delay(100);

      var sizeBeforeCleanUp = await new DirectoryInfo(tempDir).CalcDirectorySizeAsync(lifetime.Token);
      Assert.NotEqual(0, sizeBeforeCleanUp);

      await Task.Delay(TimeSpan.FromMilliseconds(1000));

      var sizeAfterCleanUp = await new DirectoryInfo(tempDir).CalcDirectorySizeAsync(lifetime.Token);
      Assert.Equal(0, sizeAfterCleanUp);
    }
    finally
    {
      p_semaphore.Release();
    }
  }

}
