#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw;

public class Lifetime : ILifetime
{
    private readonly ConcurrentStack<Func<Task>> p_doOnCompleted = new();
    private readonly CancellationTokenSource p_cts = new();
    private readonly Subject<Unit> p_flow;
    private readonly ManualResetEvent p_done;

    public Lifetime()
    {
        p_done = new ManualResetEvent(false);
        p_flow = new Subject<Unit>();
        p_flow
            .Take(1)
            .ObserveOnThreadPool()
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
                p_done.Set();
            }, ThreadPoolScheduler.Instance)
            .Subscribe(Token);
    }

    public CancellationToken Token => p_cts.Token;

    public bool CancellationRequested => p_cts.Token.IsCancellationRequested;

    public IObservable<bool> OnCompleteStarted => p_flow.Take(1).ObserveOnThreadPool().Select(_ => true);

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
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

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(parameterName: "_instance")]
    public T? DisposeAsyncOnCompleted<T>(T? _instance) where T : IAsyncDisposable
    {
        if (p_cts.Token.IsCancellationRequested)
            throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

        p_doOnCompleted.Push(async () =>
        {
            if (_instance != null)
                await _instance.DisposeAsync().AsTask();
        });
        return _instance;
    }
#endif

    public void DoOnCompleted(Func<Task> _action)
    {
        if (p_cts.Token.IsCancellationRequested)
            throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

        p_doOnCompleted.Push(_action);
    }

    public void DoOnCompleted(Action _action)
    {
        if (p_cts.Token.IsCancellationRequested)
            throw new InvalidOperationException($"This instance of {nameof(Lifetime)} is already completed!");

        p_doOnCompleted.Push(() =>
        {
            _action();
            return Task.CompletedTask;
        });
    }

    public async Task CompleteAsync()
    {
        if (p_cts.Token.IsCancellationRequested)
            return;

        p_flow.OnNext();
        await p_flow.DefaultIfEmpty();
        p_flow?.Dispose();
    }

    public void Complete()
    {
        if (p_cts.Token.IsCancellationRequested)
            return;

        p_flow.OnNext();
        p_done.WaitOne();
        p_done?.Dispose();
        p_flow?.Dispose();
    }

    public ILifetime GetChildLifetime()
    {
        var lifetime = new Lifetime();
        DoOnCompleted(lifetime.CompleteAsync);
        return lifetime;
    }

}
