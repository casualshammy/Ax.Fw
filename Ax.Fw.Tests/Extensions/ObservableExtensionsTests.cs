using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Tests.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        //Thread.Sleep(250);
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

  [Fact]
  public void DistinctUntilArrayChanged_ReturnsDistinctArrays()
  {
    var source = new[] {
      [1, 2, 3],
      [1, 2, 3],
      new[] { 4, 5, 6 }
    }.ToObservable();

    var result = source.DistinctUntilArrayChanged<int[], int>().ToEnumerable();

    Assert.Collection(result,
      _array => Assert.Equal(new[] { 1, 2, 3 }, _array),
      _array => Assert.Equal(new[] { 4, 5, 6 }, _array));
  }

  [Fact]
  public void DistinctUntilArrayChanged_HandlesNullArrays()
  {
    var source = new[]
    {
      null,
      null,
      new[] { 1, 2, 3 },
      null
    }.ToObservable();

    var result = source.DistinctUntilArrayChanged<int[]?, int>().ToEnumerable();

    Assert.Collection(result,
      Assert.Null,
      _array => Assert.Equal(new[] { 1, 2, 3 }, _array),
      Assert.Null);
  }

  [Fact]
  public void DistinctUntilArrayChanged_HandlesEmptyArrays()
  {
    var source = new[]
    {
      Array.Empty<int>(),
      Array.Empty<int>(),
      new[] { 1, 2 },
      Array.Empty<int>()
    }.ToObservable();

    var result = source.DistinctUntilArrayChanged<int[], int>().ToEnumerable();

    Assert.Collection(result,
      Assert.Empty,
      _array => Assert.Equal(new[] { 1, 2 }, _array),
      Assert.Empty);
  }

  [Fact]
  public void DistinctUntilArrayChanged_UsesCustomComparer()
  {
    var source = new[]
    {
      new[] { 1, 2 },
      new[] { 2, 1 },
      new[] { 30, 40 }
    }.ToObservable();

    var comparer = new CustomArrayComparer();

    var result = source.DistinctUntilArrayChanged<int[], int>(comparer).ToEnumerable();

    Assert.Collection(result,
      _array => Assert.Equal(new[] { 1, 2 }, _array),
      _array => Assert.Equal(new[] { 30, 40 }, _array));
  }

  [Fact]
  public void DistinctUntilArrayChanged_HandlesDifferentLengths()
  {
    var source = new[]
    {
      new[] { 1, 2 },
      new[] { 1, 2, 3 },
      new[] { 1, 2 }
    }.ToObservable();

    var result = source.DistinctUntilArrayChanged<int[], int>().ToEnumerable();

    Assert.Collection(result,
      _array => Assert.Equal(new[] { 1, 2 }, _array),
      _array => Assert.Equal(new[] { 1, 2, 3 }, _array),
      _array => Assert.Equal(new[] { 1, 2 }, _array));
  }

  [Fact]
  public void DistinctUntilArrayChanged_EmitsAllWhenNoDuplicates()
  {
    var source = new[]
    {
      new[] { 1, 2 },
      new[] { 3, 4 },
      new[] { 5, 6 }
    }.ToObservable();

    var result = source.DistinctUntilArrayChanged<int[], int>().ToEnumerable();

    Assert.Collection(result,
      _array => Assert.Equal(new[] { 1, 2 }, _array),
      _array => Assert.Equal(new[] { 3, 4 }, _array),
      _array => Assert.Equal(new[] { 5, 6 }, _array));
  }

  [Fact]
  public void DistinctUntilArrayChanged_HandlesSingleElement()
  {
    var source = new[]
    {
      new[] { 1, 2, 3 }
    }.ToObservable();

    var result = source.DistinctUntilArrayChanged<int[], int>().ToEnumerable();

    Assert.Collection(result,
      _array => Assert.Equal(new[] { 1, 2, 3 }, _array));
  }

  [Fact]
  public void DistinctUntilArrayChanged_HandlesEmptySequence()
  {
    var source = Array.Empty<int[]>().ToObservable();

    var result = source.DistinctUntilArrayChanged<int[], int>().ToEnumerable();

    Assert.Empty(result);
  }

  [Fact]
  public async Task SelectAsync_NotRaceExecutionAsync()
  {
    using var lifetime = new Lifetime();
    var list = new List<int>();

    Observable
      .Range(0, 3)
      .SelectAsync(async (_value, _ct) =>
      {
        if (_value == 0)
          await Task.Delay(2000, _ct);
        if (_value == 1)
          await Task.Delay(1000, _ct);

        list.Add(_value);
      })
      .Subscribe(lifetime);

    await Task.Delay(5000, lifetime.Token);
    Assert.Equal(3, list.Count);
    Assert.Equal(0, list[0]);
    Assert.Equal(1, list[1]);
    Assert.Equal(2, list[2]);
  }

  private class CustomArrayComparer : IEqualityComparer<int>
  {
    public bool Equals(int _x, int _y) => Math.Abs(_x - _y) < 10;

    public int GetHashCode(int _obj) => _obj;
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_FirstElementPassesImmediately()
  {
    var interval = TimeSpan.FromMilliseconds(200);
    var subject = new Subject<int>();

    var results = new List<(int Value, long ElapsedMs)>();
    var sw = Stopwatch.StartNew();

    var subscription = subject
      .SampleWithImmediate(interval)
      .Subscribe(_value => results.Add((_value, sw.ElapsedMilliseconds)));

    subject.OnNext(1);

    await Task.Delay(50);

    Assert.Single(results);
    var first = results.First();
    Assert.Equal(1, results[0].Value);
    Assert.True(first.ElapsedMs < 100, $"First element should pass immediately, elapsed: {first.ElapsedMs}ms");

    subscription.Dispose();
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_ElementDelayedWhenWithinInterval()
  {
    var interval = TimeSpan.FromMilliseconds(200);
    var subject = new Subject<int>();

    var results = new List<(int Value, long ElapsedMs)>();
    var sw = Stopwatch.StartNew();

    var subscription = subject
      .SampleWithImmediate(interval)
      .Subscribe(_value =>
      {
        lock (results)
          results.Add((_value, sw.ElapsedMilliseconds));
      });

    subject.OnNext(1);
    await Task.Delay(50);
    subject.OnNext(2);

    await Task.Delay(500);

    lock (results)
    {
      Assert.Equal(2, results.Count);
      Assert.Equal(1, results[0].Value);
      Assert.Equal(2, results[1].Value);
      var gap = results[1].ElapsedMs - results[0].ElapsedMs;
      Assert.True(gap >= 180, $"Gap should be ~200ms, was {gap}ms");
    }

    subscription.Dispose();
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_ValueReplacedDuringDelay()
  {
    var interval = TimeSpan.FromMilliseconds(300);
    var subject = new Subject<int>();

    var results = new List<int>();

    var subscription = subject
      .SampleWithImmediate(interval)
      .Subscribe(_value =>
      {
        lock (results)
          results.Add(_value);
      });

    subject.OnNext(1);
    await Task.Delay(50);
    subject.OnNext(2);
    await Task.Delay(50);
    subject.OnNext(3);

    await Task.Delay(600);

    lock (results)
    {
      Assert.Equal(2, results.Count);
      Assert.Equal(1, results[0]);
      Assert.Equal(3, results[1]);
    }

    subscription.Dispose();
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_ElementPassesImmediatelyAfterIntervalElapsed()
  {
    var interval = TimeSpan.FromMilliseconds(200);
    var subject = new Subject<int>();

    var results = new List<(int Value, long ElapsedMs)>();
    var sw = Stopwatch.StartNew();

    var subscription = subject
      .SampleWithImmediate(interval)
      .Subscribe(_value =>
      {
        lock (results)
          results.Add((_value, sw.ElapsedMilliseconds));
      });

    subject.OnNext(1);
    await Task.Delay(300);
    subject.OnNext(2);

    await Task.Delay(100);

    lock (results)
    {
      Assert.Equal(2, results.Count);
      Assert.Equal(1, results[0].Value);
      Assert.Equal(2, results[1].Value);
      var gap = results[1].ElapsedMs - results[0].ElapsedMs;
      Assert.True(gap >= 280 && gap < 350, $"Second element should pass immediately after interval, gap was {gap}ms");
    }

    subscription.Dispose();
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_CompletionEmitsPendingValue()
  {
    var interval = TimeSpan.FromMilliseconds(300);
    var subject = new Subject<int>();

    var results = new List<int>();
    var completed = false;

    var subscription = subject
      .SampleWithImmediate(interval)
      .Synchronize()
      .Subscribe(
        _value =>
        {
          lock (results)
            results.Add(_value);
        },
        () => completed = true);

    subject.OnNext(1);
    await Task.Delay(50);
    subject.OnCompleted();

    await Task.Delay(500);

    lock (results)
    {
      Assert.Single(results);
      Assert.Equal(1, results[0]);
    }
    Assert.True(completed);

    subscription.Dispose();
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_CompletionWithoutPendingCompletesImmediately()
  {
    var interval = TimeSpan.FromMilliseconds(300);
    var subject = new Subject<int>();

    var results = new List<int>();
    var completed = false;

    var subscription = subject
      .SampleWithImmediate(interval)
      .Synchronize()
      .Subscribe(
        _value =>
        {
          lock (results)
            results.Add(_value);
        },
        () => completed = true);

    subject.OnNext(1);
    await Task.Delay(400);
    subject.OnCompleted();

    await Task.Delay(100);

    lock (results)
    {
      Assert.Single(results);
      Assert.Equal(1, results[0]);
    }
    Assert.True(completed);

    subscription.Dispose();
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_ErrorPropagatedImmediately()
  {
    var interval = TimeSpan.FromMilliseconds(300);
    var subject = new Subject<int>();

    Exception? caughtError = null;

    var subscription = subject
      .SampleWithImmediate(interval)
      .Subscribe(
        _ => { },
        _error => caughtError = _error);

    var expectedError = new InvalidOperationException("Test error");
    subject.OnNext(1);
    await Task.Delay(50);
    subject.OnError(expectedError);

    await Task.Delay(100);

    Assert.Equal(expectedError, caughtError);

    subscription.Dispose();
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_EmptySourceCompletesImmediately()
  {
    var interval = TimeSpan.FromMilliseconds(300);
    var completed = false;

    Observable
      .Empty<int>()
      .SampleWithImmediate(interval)
      .Subscribe(
        _ => { },
        () => completed = true);

    await Task.Delay(100);

    Assert.True(completed);
  }

  [Fact(Timeout = 30000)]
  public async Task SampleWithImmediate_RapidBurstEmitsLastAfterInterval()
  {
    var interval = TimeSpan.FromMilliseconds(200);
    var subject = new Subject<int>();

    var results = new List<int>();

    var subscription = subject
      .SampleWithImmediate(interval)
      .Synchronize()
      .Subscribe(_value =>
      {
        lock (results)
          results.Add(_value);
      });

    subject.OnNext(1);
    await Task.Delay(20);
    subject.OnNext(2);
    await Task.Delay(20);
    subject.OnNext(3);
    await Task.Delay(20);
    subject.OnNext(4);
    await Task.Delay(20);
    subject.OnNext(5);

    await Task.Delay(400);

    lock (results)
    {
      Assert.Equal(2, results.Count);
      Assert.Equal(1, results[0]);
      Assert.Equal(5, results[1]);
    }

    subscription.Dispose();
  }
}
