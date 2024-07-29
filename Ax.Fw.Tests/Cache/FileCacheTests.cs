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

      await cache.StoreAsync(key, ms, null, true, lifetime.Token);

      if (!cache.TryGet(key, out var stream, out var meta))
        Assert.Fail();

      using (stream)
      {
        Assert.NotNull(stream);
        var cachedData = new byte[data.Length];
        await stream.ReadExactlyAsync(cachedData, lifetime.Token);
        Assert.Equal(data, cachedData);
      }
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

      await cache.StoreAsync(key, ms, null, true, lifetime.Token);

      await Task.Delay(TimeSpan.FromMilliseconds(1000));

      if (cache.TryGet(key, out var stream, out var meta))
        Assert.Fail();
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
      var cache = new FileCache(lifetime, tempDir, TimeSpan.FromDays(1), (3 * 1024) + (3 * 512), TimeSpan.FromMinutes(10));

      var data = new byte[1024];
      Random.Shared.NextBytes(data);
      using var ms = new MemoryStream();
      await ms.WriteAsync(data, lifetime.Token);

      for (var i = 0; i < 4; i++)
      {
        var key = $"test-key-{i}";
        ms.Position = 0;
        await cache.StoreAsync(key, ms, null, true, lifetime.Token);
        await Task.Delay(250);
      }

      await cache.CleanFilesWaitAsync(lifetime.Token);

      if (cache.TryGet($"test-key-0", out _, out _))
        Assert.Fail();
      if (!cache.TryGet($"test-key-1", out _, out _))
        Assert.Fail();
      if (!cache.TryGet($"test-key-2", out _, out _))
        Assert.Fail();
      if (!cache.TryGet($"test-key-3", out _, out _))
        Assert.Fail();
    }
    finally
    {
      p_semaphore.Release();
    }
  }

  [Theory(Timeout = 30000)]
  [InlineData(MimeTypes.Png)]
  [InlineData(null)]
  public async Task TestMetaAsync(string? _mime)
  {
    await p_semaphore.WaitAsync();
    try
    {
      using var lifetime = new Lifetime();
      var tempDir = Path.Combine(Path.GetTempPath(), Random.Shared.Next().ToString());
      var cache = new FileCache(lifetime, tempDir, TimeSpan.FromDays(1), 3 * 1024, TimeSpan.FromMinutes(10));

      var key = "test-key";
      var data = new byte[1024];
      Random.Shared.NextBytes(data);

      using var ms = new MemoryStream();
      await ms.WriteAsync(data, lifetime.Token);
      ms.Position = 0;

      await cache.StoreAsync(key, ms, _mime, true, lifetime.Token);

      if (!cache.TryGet(key, out _, out var meta))
        Assert.Fail();

      Assert.Equal(meta.Mime, _mime ?? MimeTypes.Bin);
    }
    finally
    {
      p_semaphore.Release();
    }
  }

  [Fact(Timeout = 30000)]
  public async Task CachedStreamLengthIsEqualToDataLengthAsync()
  {
    await p_semaphore.WaitAsync();
    try
    {
      using var lifetime = new Lifetime();
      var tempDir = Path.Combine(Path.GetTempPath(), Random.Shared.Next().ToString());
      var cache = new FileCache(lifetime, tempDir, TimeSpan.FromDays(1), 1024 * 1024, TimeSpan.FromMinutes(10));

      var data = new byte[1024];
      Random.Shared.NextBytes(data);
      using var ms = new MemoryStream();
      await ms.WriteAsync(data, lifetime.Token);

      ms.Position = 0;
      await cache.StoreAsync("test-key", ms, null, true, lifetime.Token);

      await cache.CleanFilesWaitAsync(lifetime.Token);

      if (!cache.TryGet("test-key", out var cachedStream, out var meta))
        Assert.Fail();

      using (cachedStream)
      {
        Assert.Equal(data.Length, cachedStream.Length);
        Assert.Equal(0, cachedStream.Position);
      }

      Assert.Throws<ObjectDisposedException>(() => cachedStream.Seek(5, SeekOrigin.Begin));
    }
    finally
    {
      p_semaphore.Release();
    }
  }

}
