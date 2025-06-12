using Ax.Fw.Cache;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests.Cache;

public class SyncCacheTests
{
  private SyncCache<string, int> CreateCache(
    int _capacity = 10, 
    int _overhead = 2, 
    TimeSpan? _ttl = null)
  {
    return new SyncCache<string, int>(
      new SyncCacheSettings(_capacity, _overhead, _ttl ?? TimeSpan.FromSeconds(1)));
  }

  [Fact]
  public void Put_And_TryGet_Works()
  {
    var cache = CreateCache();
    cache.Put("a", 42);

    Assert.True(cache.TryGet("a", out var value));
    Assert.Equal(42, value);
  }

  [Fact]
  public async Task TryGet_ReturnsFalse_WhenExpiredAsync()
  {
    var cache = CreateCache(_ttl: TimeSpan.FromMilliseconds(50));
    cache.Put("a", 1);

    await Task.Delay(100);

    Assert.False(cache.TryGet("a", out var value));
    Assert.Equal(default, value);
  }

  [Fact]
  public void GetOrPut_CallsFactory_IfNotExists()
  {
    var cache = CreateCache();
    int factoryCallCount = 0;

    int Factory(string _key)
    {
      factoryCallCount++;
      return 99;
    }

    var value = cache.GetOrPut("b", Factory);

    Assert.Equal(99, value);
    Assert.Equal(1, factoryCallCount);
  }

  [Fact]
  public void GetOrPut_DoesNotCallFactory_IfExists()
  {
    var cache = CreateCache();
    cache.Put("c", 123);

    static int Factory(string _key) => throw new Exception("Should not be called");

    var value = cache.GetOrPut("c", Factory);

    Assert.Equal(123, value);
  }

  [Fact]
  public async Task GetOrPutAsync_Works()
  {
    var cache = CreateCache();
    int factoryCallCount = 0;

    async Task<int> Factory(string _key)
    {
      factoryCallCount++;
      await Task.Delay(1);
      return 77;
    }

    var value = await cache.GetOrPutAsync("d", Factory);

    Assert.Equal(77, value);
    Assert.Equal(1, factoryCallCount);
  }

  [Fact]
  public void TryRemove_Removes_And_ReturnsValue()
  {
    var cache = CreateCache();
    cache.Put("e", 555);

    Assert.True(cache.TryRemove("e", out var value));
    Assert.Equal(555, value);
    Assert.False(cache.TryGet("e", out _));
  }

  [Fact]
  public async Task GetValues_Returns_Only_ValidEntriesAsync()
  {
    var cache = CreateCache(_ttl: TimeSpan.FromMilliseconds(50));
    cache.Put("x", 1);
    cache.Put("y", 2);

    await Task.Delay(100);

    cache.Put("z", 3);

    var values = cache.GetValues();

    Assert.Single(values);
    Assert.True(values.ContainsKey("z"));
  }

  [Fact]
  public void Put_Enforces_Capacity()
  {
    var cache = CreateCache(_capacity: 2, _overhead: 1, _ttl: TimeSpan.FromMinutes(1));
    cache.Put("a", 1);
    cache.Put("b", 2);
    cache.Put("c", 3);
    cache.Put("d", 4);

    Assert.Equal(2, cache.Count);
  }
}
