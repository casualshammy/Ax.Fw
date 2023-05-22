using Ax.Fw.Extensions;
using Ax.Fw.Tests.Tools;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests;

public class LifetimeTests
{
  [Fact(Timeout = 30000)]
  public void FlowTest()
  {
    var lifetime = new Lifetime();

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

    Assert.Equal(2, counter);
  }

  [Theory(Timeout = 30000)]
  [Repeat(10)]
  public void ParallelCompleteTest(int _iteration)
  {
    _ = _iteration;
    var lifetime = new Lifetime();

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

  [Fact(Timeout = 10000)]
  public void RxTest()
  {
    var lifetime = new Lifetime();

    var counter = 0L;
    lifetime.DoOnEnding(() => Interlocked.Increment(ref counter));
    lifetime.DoOnEnding(async () =>
    {
      await Task.Delay(100);
      Interlocked.Increment(ref counter);
    });

    lifetime.DoNotEndingUntilCompleted(Observable
      .Timer(TimeSpan.FromSeconds(1))
      .Select(_ =>
      {
        Interlocked.Increment(ref counter);
        return Unit.Default;
      }));

    lifetime.DoNotEndUntilCompleted(Observable
      .Timer(TimeSpan.FromSeconds(1))
      .Select(_ =>
      {
        Interlocked.Increment(ref counter);
        return Unit.Default;
      }));

    lifetime.DoNotEndUntilCompleted(Observable
      .Timer(TimeSpan.FromSeconds(1))
      .Select(_ => throw new Exception()));

    lifetime.End();

    Assert.Equal(4, Interlocked.Read(ref counter));
  }

  [Fact(Timeout = 10000)]
  public void ChildNotBlocksParent()
  {
    var lifetime = new Lifetime();
    var counter = 0L;

    var check = 0L;
    lifetime.DoOnEnding(() =>
    {
      Interlocked.Exchange(ref check, Interlocked.Read(ref counter));
      Interlocked.Increment(ref counter);
    });

    var child = lifetime.GetChildLifetime();
    Assert.NotNull(child);

    child.DoOnEnding(() => Interlocked.Increment(ref counter));

    lifetime.End();

    Assert.Equal(1, Interlocked.Read(ref check));
    Assert.Equal(2, Interlocked.Read(ref counter));
  }

}
