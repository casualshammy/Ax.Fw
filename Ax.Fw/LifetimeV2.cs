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

public class LifetimeV2 : ILifetime
{
  private readonly ConcurrentStack<Func<Task>> p_doOnEnding = new();
  private readonly ConcurrentStack<Func<Task>> p_doOnEnded = new();
  private readonly ConcurrentStack<IObservable<Unit>> p_doNotEndingUntilCompleted = new();
  private readonly ConcurrentStack<IObservable<Unit>> p_doNotEndUntilCompleted = new();
  private readonly CancellationTokenSource p_cts = new();
  private readonly ReplaySubject<Unit> p_onEnding = new(1);
  private readonly IScheduler p_scheduler;
  private long p_ending = 0;

  public LifetimeV2(IScheduler _scheduler)
  {
    p_scheduler = _scheduler;
    Scheduler = _scheduler;

    OnEnding = p_onEnding
      .Select(_ => true)
      .Publish()
      .RefCount();
  }

  public IScheduler Scheduler { get; }
  public CancellationToken Token => p_cts.Token;
  public bool IsCancellationRequested => p_cts.Token.IsCancellationRequested;
  public IObservable<bool> OnEnding { get; }

  [return: NotNullIfNotNull(parameterName: nameof(_instance))]
  public T? ToDisposeOnEnding<T>(T? _instance) where T : IDisposable
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      SafeDispose(_instance);
      return _instance;
    }

    p_doOnEnding.Push(() =>
    {
      SafeDispose(_instance);
      return Task.CompletedTask;
    });
    return _instance;
  }

  [return: NotNullIfNotNull(parameterName: nameof(_instance))]
  public T? ToDisposeAsyncOnEnding<T>(T? _instance) where T : IAsyncDisposable
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      var task = Task.Run(async () => await SafeDisposeAsync(_instance));
      task.Wait();
      return _instance;
    }

    p_doOnEnding.Push(async () => await SafeDisposeAsync(_instance));
    return _instance;
  }

  public void DoOnEnding(Func<Task> _action)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      var task = Task.Run(async () => await SafeActionAsync(_action));
      task.Wait();
      return;
    }

    p_doOnEnding.Push(() => SafeActionAsync(_action));
  }

  public void DoOnEnding(Action _action)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      SafeAction(_action);
      return;
    }

    p_doOnEnding.Push(() =>
    {
      SafeAction(_action);
      return Task.CompletedTask;
    });
  }

  public void DoNotEndingUntilCompleted(IObservable<Unit> _observable)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
      return;

    p_doNotEndingUntilCompleted.Push(_observable);
  }

  public void DoNotEndUntilCompleted(IObservable<Unit> _observable)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
      return;

    p_doNotEndUntilCompleted.Push(_observable);
  }

  public ILifetime? GetChildLifetime()
  {
    if (Interlocked.Read(ref p_ending) == 1L)
      return null;

    var lifetime = new LifetimeV2(p_scheduler);
    DoOnEnding(lifetime.End);
    return lifetime;
  }

  public void End()
  {
    if (Interlocked.Exchange(ref p_ending, 1L) == 1L)
      return;

    using var mre = new ManualResetEvent(false);

    p_scheduler.ScheduleAsync(async (_s, _ct) =>
    {
      while (p_doNotEndingUntilCompleted.TryPop(out var observable))
        await observable.IgnoreElements().LastOrDefaultAsync();

      p_cts.Cancel();
      p_onEnding.OnNext();

      while (p_doOnEnding.TryPop(out var task))
        await task();
      while (p_doOnEnded.TryPop(out var task))
        await task();

      while (p_doNotEndUntilCompleted.TryPop(out var observable))
        await observable.IgnoreElements().LastOrDefaultAsync();

      mre.Set();
    });

    mre.WaitOne();
  }

  protected virtual void Dispose(bool _disposing)
  {
    if (_disposing)
      End();
  }

  public void Dispose()
  {
    Dispose(_disposing: true);
    GC.SuppressFinalize(this);
  }

  private void SafeDispose(IDisposable? _disposable)
  {
    try
    {
      _disposable?.Dispose();
    }
    catch { }
  }

  private async Task SafeDisposeAsync(IAsyncDisposable? _disposable)
  {
    try
    {
      if (_disposable != null)
        await _disposable.DisposeAsync();
    }
    catch { }
  }

  private void SafeAction(Action _action)
  {
    try
    {
      _action();
    }
    catch { }
  }

  private async Task SafeActionAsync(Func<Task> _action)
  {
    try
    {
      await _action();
    }
    catch { }
  }

}
