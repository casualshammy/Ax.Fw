#nullable enable
using Ax.Fw.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ax.Fw
{
    public class Lifetime : ILifetime
    {
        private readonly ConcurrentStack<IDisposable?> p_disposeOnCompleted = new();
        private readonly ConcurrentStack<Action> p_doOnCompleted = new();
        private readonly object p_lock = new();
        private readonly CancellationTokenSource p_cts = new();

        public CancellationToken Token => p_cts.Token;

        public bool CancellationRequested => p_cts.Token.IsCancellationRequested;

        public T? DisposeOnCompleted<T>(T? _instance) where T : IDisposable
        {
            if (p_cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

            p_disposeOnCompleted.Push(_instance);
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
            if (p_cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");
            lock (p_lock)
            {
                p_cts.Cancel();
                while (p_disposeOnCompleted.TryPop(out var item))
                {
                    try
                    {
                        item?.Dispose();
                    }
                    catch { }
                }
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

    }
}
