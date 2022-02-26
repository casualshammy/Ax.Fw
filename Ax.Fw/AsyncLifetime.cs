#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw
{
    public class AsyncLifetime : IAsyncLifetime
    {
        private readonly ConcurrentStack<Func<Task>> p_doOnCompleted = new();
        private readonly CancellationTokenSource p_cts = new();
        private readonly Subject<Unit> p_flow;

        public AsyncLifetime()
        {
            p_flow = new Subject<Unit>();
            p_flow
                .Take(1)
                .SelectAsync(async _ =>
                {
                    if (p_cts.Token.IsCancellationRequested)
                        return;

                    p_cts.Cancel();
                    while (p_doOnCompleted.TryPop(out var item))
                    {
                        try
                        {
                            await item();
                        }
                        catch { }
                    }
                    p_flow.OnCompleted();
                    return;
                })
                .Subscribe(Token);
        }

        public CancellationToken Token => p_cts.Token;

        public bool CancellationRequested => p_cts.Token.IsCancellationRequested;

#if NETSTANDARD2_1_OR_GREATER
        [return: NotNullIfNotNull(parameterName: "_instance")]
#endif
        public T? DisposeOnCompleted<T>(T? _instance) where T : IDisposable
        {
            if (p_cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

            p_doOnCompleted.Push(() =>
            {
                _instance?.Dispose();
                return Task.CompletedTask;
            });
            return _instance;
        }

        public void DoOnCompleted(Func<Task> _action)
        {
            if (p_cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(AsyncLifetime)} is already completed!");

            p_doOnCompleted.Push(_action);
        }

        public void DoOnCompleted(Action _action)
        {
            if (p_cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(AsyncLifetime)} is already completed!");

            p_doOnCompleted.Push(() =>
            {
                _action();
                return Task.CompletedTask;
            });
        }

        public async Task CompleteAsync()
        {
            p_flow.OnNext(Unit.Default);
            await p_flow.DefaultIfEmpty();
            p_flow?.Dispose();
        }

        public IAsyncLifetime GetChildLifetime()
        {
            var lifetime = new AsyncLifetime();
            DoOnCompleted(lifetime.CompleteAsync);
            return lifetime;
        }

    }
}
