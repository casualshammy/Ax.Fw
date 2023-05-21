#nullable enable
using Ax.Fw.Tests.Tools;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests;

public class LifetimeTests
{
  [Fact(Timeout = 30000)]
  public async Task FlowTest()
  {
    var lifetime = new Lifetime();

    var counter = 0;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      Interlocked.Increment(ref counter);
      await Task.Delay(100);
    });

    await lifetime.EndAsync();

    Assert.Equal(2, counter);

  }

  [Fact(Timeout = 30000)]
  public void SyncCompleteTest()
  {
    var lifetime = new Lifetime();

    var counter = 0;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      Interlocked.Increment(ref counter);
      await Task.Delay(500);
    });

    lifetime.End();

    Assert.Equal(2, counter);

  }

  [Fact(Timeout = 30000)]
  public void MultipleCompleteTest()
  {
    var lifetime = new Lifetime();

    var counter = 0;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      Interlocked.Increment(ref counter);
      await Task.Delay(500);
    });

    lifetime.End();
    lifetime.End();

    Assert.Equal(2, counter);

    Thread.Sleep(500);
    lifetime.End();
    lifetime.End();
  }

  [Fact(Timeout = 30000)]
  public void ParallelCompleteTest()
  {
    var lifetime = new Lifetime();

    var counter = 0;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      Interlocked.Increment(ref counter);
      await Task.Delay(500);
    });

    Parallel.For(0, 100, _ =>
    {
      lifetime.End();
    });
    Thread.Sleep(500);
    Parallel.For(0, 100, _ =>
    {
      lifetime.End();
    });

    Assert.Equal(2, counter);
  }

  [Theory(Timeout = 30000)]
  [Repeat(10)]
  public async Task ParallelCompleteAsyncTest(int _iteration)
  {
    _ = _iteration;
    var lifetime = new Lifetime();

    var counter = 0;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      Interlocked.Increment(ref counter);
      await Task.Delay(1000);
    });

    _ = Task.Factory.StartNew(() => lifetime.EndAsync(), TaskCreationOptions.LongRunning);
    _ = Task.Factory.StartNew(() => lifetime.EndAsync(), TaskCreationOptions.LongRunning);
    _ = Task.Factory.StartNew(() => lifetime.EndAsync(), TaskCreationOptions.LongRunning);

    await Task.Delay(250);
    Assert.Equal(1, counter);

    await Task.Delay(2000);
    Assert.Equal(2, counter);
  }


}
