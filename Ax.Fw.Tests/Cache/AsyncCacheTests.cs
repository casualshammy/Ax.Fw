using Ax.Fw.Cache;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests.Cache;

public class AsyncCacheTests
{
  [Fact(Timeout = 30000)]
  public async Task NotTimeoutTestAsync()
  {
    using var lifetime = new Lifetime();

    var counter = 0;
    async Task<string?> generator(CancellationToken _ct)
    {
      var c = Interlocked.Increment(ref counter);
      return c.ToString();
    }

    var cachedValue = new AsyncCachedValue<string>(TimeSpan.FromMinutes(1), generator);

    var v0 = await cachedValue.GetValueAsync(lifetime.Token);
    var v1 = await cachedValue.GetValueAsync(lifetime.Token);

    Assert.Equal(1, counter);
    Assert.Equal(v0, v1);
    Assert.Equal("1", v0);
  }

  [Fact(Timeout = 30000)]
  public async Task TimeoutTestAsync()
  {
    using var lifetime = new Lifetime();

    var counter = 0;
    async Task<string?> generator(CancellationToken _ct)
    {
      var c = Interlocked.Increment(ref counter);
      return c.ToString();
    }

    var cachedValue = new AsyncCachedValue<string>(TimeSpan.FromSeconds(1), generator);

    var v0 = await cachedValue.GetValueAsync(lifetime.Token);
    await Task.Delay(TimeSpan.FromSeconds(2));
    var v1 = await cachedValue.GetValueAsync(lifetime.Token);

    Assert.Equal(2, counter);
    Assert.NotEqual(v0, v1);
    Assert.Equal("2", v1);
  }
}
