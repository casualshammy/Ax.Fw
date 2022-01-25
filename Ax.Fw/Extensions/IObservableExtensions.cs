using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions
{
    public static class IObservableExtensions
    {
        public static IObservable<TOut> SelectAsync<TIn, TOut>(this IObservable<TIn> _this, Func<TIn, Task<TOut>> _selector)
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

        public static IObservable<Unit> Select<TIn>(this IObservable<TIn> _this, Action<TIn> _selector)
        {
            return _this.Select(x =>
            {
                _selector(x);
                return Unit.Default;
            });
        }

    }
}
