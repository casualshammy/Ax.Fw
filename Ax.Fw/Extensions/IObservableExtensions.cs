using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions;

public static class IObservableExtensions
{
  record AliveCtx<T>(ILifetime? Lifetime, T? Value);

  public static IObservable<TOut> SelectAsync<TIn, TOut>(this IObservable<TIn> _this, Func<TIn, Task<TOut>> _selector)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(() => _selector(_x));
        })
        .Concat();
  }

  public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn> _this, Func<TIn, Task> _selector)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(() => _selector(_x));
        })
        .Concat();
  }

  public static IObservable<TOut> SelectAsync<TIn, TOut>(this IObservable<TIn> _this, Func<TIn, Task<TOut>> _selector, IScheduler _scheduler)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(() => _selector(_x), _scheduler);
        })
        .Concat();
  }

  public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn> _this, Func<TIn, Task> _selector, IScheduler _scheduler)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(() => _selector(_x), _scheduler);
        })
        .Concat();
  }

  public static IObservable<TOut> SelectAsync<TIn, TOut>(this IObservable<TIn> _this, Func<TIn, CancellationToken, Task<TOut>> _selector)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(_c => _selector(_x, _c));
        })
        .Concat();
  }

  public static IObservable<TOut> SelectAsync<TIn, TOut>(this IObservable<TIn> _this, Func<TIn, CancellationToken, Task<TOut>> _selector, IScheduler _scheduler)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(_c => _selector(_x, _c), _scheduler);
        })
        .Concat();
  }

  public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn> _this, Func<TIn, CancellationToken, Task> _selector)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(_c => _selector(_x, _c));
        })
        .Concat();
  }

  public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn> _this, Func<TIn, CancellationToken, Task> _selector, IScheduler _scheduler)
  {
    return _this
        .Select(_x =>
        {
          return Observable.FromAsync(_c => _selector(_x, _c), _scheduler);
        })
        .Concat();
  }

  public static IObservable<Unit> Select<TIn>(this IObservable<TIn?> _this, Action<TIn?> _selector)
  {
    return _this.Select(_x =>
    {
      _selector(_x);
      return Unit.Default;
    });
  }

  public static IObservable<T> WhereNotNull<T>(this IObservable<T?> _this)
  {
    return _this.Where(_x => _x != null)!;
  }

  public static IObservable<T?> StartWithDefault<T>(this IObservable<T?> _this)
  {
    return Observable
        .Return(default(T))
        .Concat(_this);
  }

  public static IObservable<Unit> ToUnit<T>(this IObservable<T?> _this)
  {
    return _this
        .Select(_ => Unit.Default);
  }

  public static void OnNext(this ISubject<Unit> _this) => _this.OnNext(Unit.Default);

  public static void Subscribe<T>(this IObservable<T> _observable, Action<T> _handler, IReadOnlyLifetime _lifetime)
  {
    _lifetime.ToDisposeOnEnding(_observable.Subscribe(_handler));
  }

  public static void Subscribe<T>(this IObservable<T> _observable, IReadOnlyLifetime _lifetime)
  {
    _lifetime.ToDisposeOnEnding(_observable.Subscribe());
  }

  public static IRxProperty<T> ToProperty<T>(this IObservable<T> _observable, IReadOnlyLifetime _lifetime, T _defaultValue) where T : notnull
  {
    return new RxProperty<T>(_observable, _lifetime, _defaultValue);
  }

  public static IRxProperty<T?> ToNullableProperty<T>(this IObservable<T> _observable, IReadOnlyLifetime _lifetime, T? _defaultValue = default)
  {
    return new RxProperty<T?>(_observable, _lifetime, _defaultValue);
  }

  public static IObservable<TResult> ScanAsync<T, TResult>(this IObservable<T> _observable, TResult _seed, Func<TResult, T, Task<TResult>> _functor)
  {
    return _observable
        .Scan(
            Task.FromResult(_seed),
            async (_prev, _newEntry) =>
            {
              var prevState = await _prev.ConfigureAwait(false);
              return await _functor(prevState, _newEntry).ConfigureAwait(false);
            })
        .Concat();
  }

  public static IObservable<T> ObserveOnThreadPool<T>(this IObservable<T> _observable)
      => _observable.ObserveOn(ThreadPoolScheduler.Instance);

  public static IObservable<ImmutableHashSet<T>> DistinctUntilHashSetChanged<T>(this IObservable<ImmutableHashSet<T>> _observable)
      => _observable.DistinctUntilChanged(ImmutableHashSetComparer<T>.Default);

  public static IObservable<A> DistinctUntilArrayChanged<A, T>(
    this IObservable<A> _observable,
    IEqualityComparer<T>? _entryComparer = null)
    where A : IList<T>?
  {
    var comparer = _entryComparer ?? EqualityComparer<T>.Default;

    return _observable
      .DistinctUntilChanged(CommonUtilities.CreateEqualityComparer<A>((_a, _b) =>
      {
        if (_a == null && _b == null)
          return true;

        if (_a == null || _b == null)
          return false;

        if (_a.Count != _b.Count)
          return false;

        for (var i = 0; i < _a.Count; i++)
          if (!comparer.Equals(_a[i], _b[i]))
            return false;

        return true;
      }));
  }

  public static void HotAlive<T>(
    this IObservable<T> _this,
    IReadOnlyLifetime _lifetime,
    IScheduler? _scheduler,
    Action<T, IReadOnlyLifetime> _functor)
  {
    _this
      .Alive(_lifetime, _scheduler, (_entry, _life) =>
      {
        _functor(_entry, _life);
        return Unit.Default;
      })
      .Subscribe(_lifetime);
  }

  public static IObservable<TOut?> Alive<TIn, TOut>(
    this IObservable<TIn> _this,
    IReadOnlyLifetime _lifetime,
    IScheduler? _scheduler,
    Func<TIn, IReadOnlyLifetime, TOut> _functor)
  {
    IObservable<TIn> observable;
    if (_scheduler == null)
      observable = _this;
    else
      observable = _this.ObserveOn(_scheduler);

    return observable
      .Scan(new AliveCtx<TOut>(null, default), (_acc, _entry) =>
      {
        _acc.Lifetime?.End();
        var life = _lifetime.GetChildLifetime();
        if (life == null)
          return _acc with { Lifetime = null };

        var result = _functor.Invoke(_entry, life);
        return new AliveCtx<TOut>(life, result);
      })
      .Select(_ => _.Value);
  }

  public static IObservable<TResult> ObserveAndTransformLatestOn<TSource, TResult>(
    this IObservable<TSource> _observable,
    IScheduler _scheduler,
    Func<TSource, CancellationToken, Task<TResult>> _transform)
  {
    return Observable.Create<TResult>(_observer =>
    {
      var channel = Channel.CreateBounded<TSource>(new BoundedChannelOptions(1)
      {
        FullMode = BoundedChannelFullMode.DropOldest
      });

      var notificationSubj = new Subject<Unit>();
      var waitingNotificationsCount = 0L;

      var handlerSubs = notificationSubj
        .ObserveOn(Scheduler.Default)
        .SelectAsync(async (_, _ct) =>
        {
          try
          {
            if (_ct.IsCancellationRequested)
              return;

            if (!channel.Reader.TryRead(out var item))
              return;

            var result = await _transform(item, _ct);
            _observer.OnNext(result);
          }
          catch (Exception ex)
          {
            _observer.OnError(ex);
          }
          finally
          {
            Interlocked.Decrement(ref waitingNotificationsCount);
          }
        }, _scheduler)
        .Subscribe();

      var sourceSubs = _observable
        .Subscribe(_item =>
        {
          // не может сфэйлиться из-за политики обновления
          channel.Writer.TryWrite(_item);

          // в очереди достаточно уведомлений, больше не требуется
          if (Interlocked.Read(ref waitingNotificationsCount) >= 100)
            return;

          // посылаем уведомление
          Interlocked.Increment(ref waitingNotificationsCount);
          notificationSubj.OnNext(Unit.Default);
        }, () =>
        {
          int maxWaitIterations = 50; // 5 sec
          // мы должны дать время последнему уведомлению прожеваться
          while (Interlocked.Read(ref waitingNotificationsCount) > 0 && maxWaitIterations-- > 0)
            Thread.Sleep(100);

          _observer.OnCompleted();
        });

      return new CompositeDisposable(notificationSubj, sourceSubs, handlerSubs);
    });
  }

  public static async Task<T?> FirstOrDefaultAsync<T>(
    this IObservable<T> _observable,
    CancellationToken _ct)
  {
    T? result = default;

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(_ct);

    _observable
      .Take(1)
      .Subscribe(_ =>
      {
        result = _;
        cts.Cancel();
      }, cts.Token);

    try
    {
      await Task.Delay(-1, cts.Token);
    }
    catch (OperationCanceledException) { }

    return result;
  }

  static class ImmutableHashSetComparer<T>
  {
    private class Comparer : IEqualityComparer<ImmutableHashSet<T>>
    {
      public bool Equals(ImmutableHashSet<T>? _a, ImmutableHashSet<T>? _b)
      {
        return
          _a != null &&
          _b != null &&
          _a.SetEquals(_b);
      }

      public int GetHashCode(ImmutableHashSet<T> _obj) => _obj.GetHashCode();
    }

    public static IEqualityComparer<ImmutableHashSet<T>> Default { get; } = new Comparer();

  }

}
