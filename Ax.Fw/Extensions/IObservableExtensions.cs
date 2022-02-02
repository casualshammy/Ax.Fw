#nullable enable
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions
{
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
            return _this
                .Where(_x => _x != null)
                .Select(_x => _x!);
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

        public static void Subscribe<T>(this IObservable<T> _observable, Action<T> _handler, ILifetime _lifetime)
        {
            _lifetime.DisposeOnCompleted(_observable.Subscribe(_handler));
        }

    }
}
