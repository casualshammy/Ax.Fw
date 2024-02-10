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

namespace Ax.Fw;

public class Lifetime : ILifetime
{
  private readonly ConcurrentStack<Func<Task>> p_doOnEnding = new();
  private readonly ConcurrentStack<Func<Task>> p_doOnEnded = new();
  private readonly ConcurrentStack<IObservable<Unit>> p_doNotEndingUntilCompleted = new();
  private readonly ConcurrentStack<IObservable<Unit>> p_doNotEndUntilCompleted = new();
  private readonly CancellationTokenSource p_cts = new();
  private readonly ReplaySubject<Unit> p_onEnding = new(1);
  private long p_ending = 0;

  public Lifetime()
  {
    OnEnding = p_onEnding;
  }

  public CancellationToken Token => p_cts.Token;
  public bool IsCancellationRequested => p_cts.Token.IsCancellationRequested;
  public IObservable<Unit> OnEnding { get; }

  [return: NotNullIfNotNull(parameterName: nameof(_instance))]
  public T? ToDisposeOnEnding<T>(T? _instance) where T : IDisposable
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      Lifetime.SafeDispose(_instance);
      return _instance;
    }

    p_doOnEnding.Push(() =>
    {
      Lifetime.SafeDispose(_instance);
      return Task.CompletedTask;
    });
    return _instance;
  }

  [return: NotNullIfNotNull(parameterName: "_instance")]
  public T? ToDisposeAsyncOnEnding<T>(T? _instance) where T : IAsyncDisposable
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      var task = Task.Run(async () => await Lifetime.SafeDisposeAsync(_instance));
      task.Wait();
      return _instance;
    }

    p_doOnEnding.Push(async () => await Lifetime.SafeDisposeAsync(_instance));
    return _instance;
  }

  [return: NotNullIfNotNull(parameterName: "_instance")]
  public T? ToDisposeOnEnded<T>(T? _instance) where T : IDisposable
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      Lifetime.SafeDispose(_instance);
      return _instance;
    }

    p_doOnEnded.Push(() =>
    {
      Lifetime.SafeDispose(_instance);
      return Task.CompletedTask;
    });
    return _instance;
  }

  [return: NotNullIfNotNull(parameterName: "_instance")]
  public T? ToDisposeAsyncOnEnded<T>(T? _instance) where T : IAsyncDisposable
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      var task = Task.Run(async () => await Lifetime.SafeDisposeAsync(_instance));
      task.Wait();
      return _instance;
    }

    p_doOnEnded.Push(async () => await Lifetime.SafeDisposeAsync(_instance));
    return _instance;
  }

  public void DoOnEnding(Func<Task> _action)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      var task = Task.Run(async () => await Lifetime.SafeActionAsync(_action));
      task.Wait();
      return;
    }

    p_doOnEnding.Push(() => Lifetime.SafeActionAsync(_action));
  }

  public void DoOnEnding(Action _action)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      Lifetime.SafeAction(_action);
      return;
    }

    p_doOnEnding.Push(() =>
    {
      Lifetime.SafeAction(_action);
      return Task.CompletedTask;
    });
  }

  public void DoOnEnded(Action _action)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      Lifetime.SafeAction(_action);
      return;
    }

    p_doOnEnded.Push(() =>
    {
      Lifetime.SafeAction(_action);
      return Task.CompletedTask;
    });
  }

  public void DoOnEnded(Func<Task> _action)
  {
    if (Interlocked.Read(ref p_ending) == 1L)
    {
      var task = Task.Run(async () => await Lifetime.SafeActionAsync(_action));
      task.Wait();
      return;
    }

    p_doOnEnded.Push(() => Lifetime.SafeActionAsync(_action));
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

    var lifetime = new Lifetime();
    DoOnEnding(lifetime.End);
    return lifetime;
  }

  public void InstallConsoleCtrlCHook()
  {
    void onCancelKeyPress(object? _o, ConsoleCancelEventArgs _e)
    {
      _e.Cancel = true;
      Console.CancelKeyPress -= onCancelKeyPress;
      End();
    }
    Console.CancelKeyPress += onCancelKeyPress;
  }

  public void End()
  {
    if (Interlocked.Exchange(ref p_ending, 1L) == 1L)
      return;

    using var semaphore = new SemaphoreSlim(0, 1);

    _ = Task.Run(async () =>
    {
      while (p_doNotEndingUntilCompleted.TryPop(out var observable))
        await observable
          .Catch(Observable.Return(Unit.Default))
          .IgnoreElements()
          .LastOrDefaultAsync();

      p_cts.Cancel();
      p_onEnding.OnNext();

      while (p_doOnEnding.TryPop(out var task))
        await task();

      while (p_doNotEndUntilCompleted.TryPop(out var observable))
        await observable
          .Catch(Observable.Return(Unit.Default))
          .IgnoreElements()
          .LastOrDefaultAsync();

      while (p_doOnEnded.TryPop(out var task))
        await task();

      semaphore.Release();
    });

    semaphore.Wait();
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

  private static void SafeDispose(IDisposable? _disposable)
  {
    try
    {
      _disposable?.Dispose();
    }
    catch { }
  }

  private static async Task SafeDisposeAsync(IAsyncDisposable? _disposable)
  {
    try
    {
      if (_disposable != null)
        await _disposable.DisposeAsync();
    }
    catch { }
  }

  private static void SafeAction(Action _action)
  {
    try
    {
      _action();
    }
    catch { }
  }

  private static async Task SafeActionAsync(Func<Task> _action)
  {
    try
    {
      await _action();
    }
    catch { }
  }

}
