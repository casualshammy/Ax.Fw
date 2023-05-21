using Ax.Fw.Extensions;
using Ax.Fw.Tests.Tools;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests;

public class LifetimeV2Tests
{
  [Fact(Timeout = 30000)]
  public void FlowTest()
  {
    using var scheduler = new EventLoopScheduler();
    var lifetime = new LifetimeV2(scheduler);

    var counter = 0;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      Interlocked.Increment(ref counter);
      await Task.Delay(100);
    });

    lifetime.End();

    Assert.Equal(2, counter);
  }

  [Fact(Timeout = 30000)]
  public void SyncCompleteTest()
  {
    using var scheduler = new EventLoopScheduler();
    var lifetime = new LifetimeV2(scheduler);

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
    using var scheduler = new EventLoopScheduler();
    var lifetime = new LifetimeV2(scheduler);

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

    Assert.Equal(2, counter);
  }

  [Theory(Timeout = 30000)]
  [Repeat(10)]
  public void ParallelCompleteTest(int _iteration)
  {
    _ = _iteration;
    using var scheduler = new EventLoopScheduler();
    var lifetime = new LifetimeV2(scheduler);

    var counter = 0;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      Interlocked.Increment(ref counter);
      await Task.Delay(500);
    });

    Parallel.For(0, 100, _ => lifetime.End());
    Thread.Sleep(500);
    Parallel.For(0, 100, _ => lifetime.End());

    Assert.Equal(2, counter);
  }

  [Fact(Timeout =10000)]
  public void RxTest()
  {
    using var scheduler = new EventLoopScheduler();
    var lifetime = new LifetimeV2(scheduler);

    var counter = 0L;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      await Task.Delay(100);
      Interlocked.Increment(ref counter);
    });

    lifetime.DoNotEndingUntilCompleted(Observable
      .Timer(TimeSpan.FromSeconds(0.5))
      .Select(_ =>
      {
        Interlocked.Increment(ref counter);
        return Unit.Default;
      }));

    lifetime.DoNotEndUntilCompleted(Observable
      .Timer(TimeSpan.FromSeconds(0.5))
      .Select(_ =>
      {
        Interlocked.Increment(ref counter);
        return Unit.Default;
      }));

    lifetime.End();

    Assert.Equal(4, Interlocked.Read(ref counter));
  }

}
