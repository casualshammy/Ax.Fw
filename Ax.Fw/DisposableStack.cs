using Ax.Fw.SharedTypes.Interfaces;
using System;

namespace Ax.Fw;

/// <summary>
/// Base class for objects that need to tie the lifetime of multiple <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>
/// resources to their own lifetime. Registered resources are disposed when the stack is disposed.
/// </summary>
public abstract class DisposableStack : IDisposable
{
  private readonly ILifetime p_lifetime = new Lifetime();
  private volatile bool p_disposedValue;

  /// <summary>
  /// Registers an <see cref="IDisposable"/> for disposal when this stack is being disposed.
  /// </summary>
  protected T ToDispose<T>(T _value) where T : IDisposable => p_lifetime.ToDisposeOnEnding(_value);

  /// <summary>
  /// Registers an <see cref="IAsyncDisposable"/> for asynchronous disposal when this stack is being disposed.
  /// </summary>
  protected T ToDisposeAsync<T>(T _value) where T : IAsyncDisposable => p_lifetime.ToDisposeAsyncOnEnding(_value);

  /// <summary>
  /// Registers an <see cref="IDisposable"/> that will be disposed when the stack ends.
  /// </summary>
  protected T ToDisposeOnEnded<T>(T _value) where T : IDisposable => p_lifetime.ToDisposeOnEnded(_value);

  /// <summary>
  /// Schedules an action to be executed when this stack is being disposed.
  /// </summary>
  protected void ToDoOnDisposing(Action _action) => p_lifetime.DoOnEnding(_action);

  /// <summary>
  /// Schedules an action to be executed when the stack ends.
  /// </summary>
  protected void ToDoOnEnded(Action _action) => p_lifetime.DoOnEnded(_action);

  /// <inheritdoc cref="IDisposable.Dispose" />
  protected virtual void Dispose(bool _disposing)
  {
    if (!p_disposedValue)
    {
      if (_disposing)
        p_lifetime.End();

      p_disposedValue = true;
    }
  }

  /// <summary>
  /// Disposes the stack and all registered resources. Callers should use this method
  /// or a <c>using</c> statement to trigger cleanup.
  /// </summary>
  public void Dispose()
  {
    Dispose(_disposing: true);
    GC.SuppressFinalize(this);
  }

}
