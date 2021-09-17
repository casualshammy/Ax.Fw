using Ax.Fw.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ax.Fw
{
    public class Lifetime : ILifetime
    {
        private readonly ConcurrentStack<IDisposable> p_disposeOnCompleted = new ConcurrentStack<IDisposable>();
        private readonly ConcurrentStack<Action> p_doOnCompleted = new ConcurrentStack<Action>();
        private readonly object p_lock = new();
        private readonly CancellationTokenSource cts = new();

        public CancellationToken Token => cts.Token;

        public T DisposeOnCompleted<T>(T _instance) where T : IDisposable
        {
            if (cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

            p_disposeOnCompleted.Push(_instance);
            return _instance;
        }

        public void DoOnCompleted(Action _action)
        {
            if (cts.Token.IsCancellationRequested)
                throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

            p_doOnCompleted.Push(_action);
        }

        public void Complete()
        {
            lock (p_lock)
            {
                cts.Cancel();
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
