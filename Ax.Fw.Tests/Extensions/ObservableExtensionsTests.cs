﻿using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Tests.Tools;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Extensions;

public class ObservableExtensionsTests
{
  private readonly ITestOutputHelper p_output;

  public ObservableExtensionsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 30000)]
  public async Task HotAliveTest()
  {
    using var lifetime = new Lifetime();

    var observableElementsCount = 5;

    var observable = Observable
      .Return(Unit.Default)
      .Repeat(observableElementsCount)
      .Publish()
      .RefCount();

    var counter = 0L;
    var lifeCompleteCounter = 0L;
    var life = (IReadOnlyLifetime?)null;

    observable
      .HotAlive(lifetime, null, (_entry, _life) =>
      {
        var oldLife = Interlocked.Exchange(ref life, _life);
        Assert.NotEqual(oldLife, _life);
        _life.DoOnEnding(() => Interlocked.Increment(ref lifeCompleteCounter));
        Interlocked.Increment(ref counter);
      });

    await Task.Delay(1000);

    Assert.Equal(observableElementsCount, counter);
    Assert.Equal(observableElementsCount - 1, lifeCompleteCounter);
  }

  [Fact(Timeout = 30000)]
  public async Task HotAlive_OverflowTest()
  {
    using var lifetime = new Lifetime();
    var lifetimeQueue = new ConcurrentQueue<IReadOnlyLifetime>();
    const int entriesCount = 10;

    var subj = new Subject<Unit>();

    _ = Parallel.ForAsync(0, entriesCount, async (_, _c) =>
    {
      await Task.Delay(250);
      subj.OnNext(Unit.Default);
    });

    subj
      .Take(entriesCount)
      .HotAlive(lifetime, new EventLoopScheduler(), (_, _life) =>
      {
        lifetimeQueue.Enqueue(_life);

        Thread.Sleep(250);
      });

    await Task.Delay(5000);

    Assert.Equal(entriesCount, lifetimeQueue.Count);

    var endedLifetimesCount = 0;
    while (lifetimeQueue.TryDequeue(out var entry))
      if (entry.IsCancellationRequested)
        ++endedLifetimesCount;

    Assert.Equal(entriesCount - 1, endedLifetimesCount);
  }

  [Fact(Timeout = 30000)]
  public async Task AliveTest()
  {
    using var lifetime = new Lifetime();

    var observableElementsCount = 5;

    var observable = Observable
      .Return(Unit.Default)
      .Repeat(observableElementsCount)
      .Publish()
      .RefCount();

    var counter = 0L;
    var lifeCompleteCounter = 0L;
    var life = (IReadOnlyLifetime?)null;

    observable
      .Alive(lifetime, null, (_entry, _life) =>
      {
        var oldLife = Interlocked.Exchange(ref life, _life);
        Assert.NotEqual(oldLife, _life);
        _life.DoOnEnding(() => Interlocked.Increment(ref lifeCompleteCounter));
        return 1;
      })
      .Subscribe(_ => Interlocked.Add(ref counter, _), lifetime);

    await Task.Delay(1000);

    Assert.Equal(observableElementsCount, counter);
    Assert.Equal(observableElementsCount - 1, lifeCompleteCounter);
  }

  [Fact(Timeout = 30000)]
  public async Task ObserveAndTrasformLatestOn_EmptyTest()
  {
    using var lifetime = new Lifetime();
    var scheduler = lifetime.ToDisposeOnEnding(new EventLoopScheduler());

    var counter = 0L;

    Observable
      .Empty<Unit>()
      .ObserveAndTransformLatestOn(scheduler, (_, _ct) =>
      {
        return Task.FromResult(Interlocked.Increment(ref counter));
      })
      .Subscribe(lifetime);

    await Task.Delay(1000);

    Assert.Equal(0L, Interlocked.Read(ref counter));
  }

  [Fact(Timeout = 30000)]
  public async Task ObserveAndTrasformLatestOn_GracefullyCloseTest()
  {
    using var lifetime = new Lifetime();
    var scheduler = lifetime.ToDisposeOnEnding(new EventLoopScheduler());

    var counter = 0L;
    var completed = false;

    Debug.WriteLine($"0: {Environment.CurrentManagedThreadId}");

    Observable
      .Return(Unit.Default)
      .ObserveAndTransformLatestOn(scheduler, (_, _ct) =>
      {
        return Task.FromResult(Interlocked.Increment(ref counter));
      })
      .Subscribe(_ => { }, () => completed = true);

    await Task.Delay(1000);

    Assert.True(completed);
    Assert.Equal(1L, Interlocked.Read(ref counter));

  }

  [Theory(Timeout = 10000)]
  [Repeat(10)]
  public async Task FirstOrDefaultAsync_BasicTestAsync(int _repeat)
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

    var data = _repeat * 100;

    {
      var result = await Observable
        .Return(data)
        .FirstOrDefaultAsync(cts.Token);

      Assert.Equal(data, result);
    }

    {
      var result = await Observable
        .Timer(TimeSpan.FromSeconds(1))
        .Select(_ => data)
        .FirstOrDefaultAsync(cts.Token);

      Assert.Equal(data, result);
    }

    {
      var result = await Observable
        .Timer(TimeSpan.FromSeconds(5))
        .Select(_ => data)
        .FirstOrDefaultAsync(cts.Token);

      Assert.Equal(default, result);
    }

  }

}
