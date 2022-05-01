#nullable enable
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions;

public static class IObservableExtensions
{
    public static IObservable<TOut?> SelectAsync<TIn, TOut>(this IObservable<TIn?> _this, Func<TIn?, Task<TOut?>> _selector)
    {
        return _this
            .Select(_x =>
            {
                return Observable.FromAsync(() => _selector(_x));
            })
            .Concat();
    }

    public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn?> _this, Func<TIn?, Task> _selector)
    {
        return _this
            .Select(_x =>
            {
                return Observable.FromAsync(() => _selector(_x));
            })
            .Concat();
    }

    public static IObservable<TOut?> SelectAsync<TIn, TOut>(this IObservable<TIn?> _this, Func<TIn?, Task<TOut?>> _selector, IScheduler _scheduler)
    {
        return _this
            .Select(_x =>
            {
                return Observable.FromAsync(() => _selector(_x), _scheduler);
            })
            .Concat();
    }

    public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn?> _this, Func<TIn?, Task> _selector, IScheduler _scheduler)
    {
        return _this
            .Select(_x =>
            {
                return Observable.FromAsync(() => _selector(_x), _scheduler);
            })
            .Concat();
    }

    public static IObservable<TOut?> SelectAsync<TIn, TOut>(this IObservable<TIn?> _this, Func<TIn?, CancellationToken, Task<TOut?>> _selector)
    {
        return _this
            .Select(_x =>
            {
                return Observable.FromAsync(_c => _selector(_x, _c));
            })
            .Concat();
    }

    public static IObservable<TOut?> SelectAsync<TIn, TOut>(this IObservable<TIn?> _this, Func<TIn?, CancellationToken, Task<TOut?>> _selector, IScheduler _scheduler)
    {
        return _this
            .Select(_x =>
            {
                return Observable.FromAsync(_c => _selector(_x, _c), _scheduler);
            })
            .Concat();
    }

    public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn?> _this, Func<TIn?, CancellationToken, Task> _selector)
    {
        return _this
            .Select(_x =>
            {
                return Observable.FromAsync(_c => _selector(_x, _c));
            })
            .Concat();
    }

    public static IObservable<Unit> SelectAsync<TIn>(this IObservable<TIn?> _this, Func<TIn?, CancellationToken, Task> _selector, IScheduler _scheduler)
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
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return _this
            .Where(_x => _x != null);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
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

    public static void OnNext(this Subject<Unit> _this)
    {
        _this.OnNext(Unit.Default);
    }

    public static void Subscribe<T>(this IObservable<T> _observable, Action<T> _handler, IReadOnlyLifetime _lifetime)
    {
        _lifetime.DisposeOnCompleted(_observable.Subscribe(_handler));
    }

    public static void Subscribe<T>(this IObservable<T> _observable, IReadOnlyLifetime _lifetime)
    {
        _lifetime.DisposeOnCompleted(_observable.Subscribe());
    }

    public static IRxProperty<T?> ToProperty<T>(this IObservable<T?> _observable, IReadOnlyLifetime _lifetime, T? _defaultValue = default)
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

}
