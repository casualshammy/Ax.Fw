#nullable enable
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Threading;

namespace Ax.Fw
{
    public class Lifetime : ILifetime
    {
        private readonly ConcurrentStack<Action> p_doOnCompleted = new();
        private readonly object p_lock = new();
        private readonly CancellationTokenSource p_cts = new();
        private readonly Subject<bool> p_onCompleteStartedFlow = new();

        public CancellationToken Token => p_cts.Token;

        public bool CancellationRequested => p_cts.Token.IsCancellationRequested;

        public IObservable<bool> OnCompleteStarted => p_onCompleteStartedFlow;

#if NETSTANDARD2_1_OR_GREATER
        [return: NotNullIfNotNull(parameterName: "_instance")]
#endif
        public T? DisposeOnCompleted<T>(T? _instance) where T : IDisposable
        {
            if (p_cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

            p_doOnCompleted.Push(() => _instance?.Dispose());
            return _instance;
        }

        public void DoOnCompleted(Action _action)
        {
            if (p_cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

            p_doOnCompleted.Push(_action);
        }

        public void Complete()
        {
            lock (p_lock)
            {
                if (p_cts.Token.IsCancellationRequested)
                    return;

                p_onCompleteStartedFlow.OnNext(true);
                p_onCompleteStartedFlow.OnCompleted();

                p_cts.Cancel();
                while (p_doOnCompleted.TryPop(out var item))
                {
                    try
                    {
                        item();
                    }
                    catch { }
                }
            }
        }

        public ILifetime GetChildLifetime()
        {
            var lifetime = new Lifetime();
            DoOnCompleted(lifetime.Complete);
            return lifetime;
        }

    }
}
