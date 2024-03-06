using Ax.Fw.SharedTypes.Interfaces;
using System;

namespace Ax.Fw;

public abstract class DisposableStack : IDisposable
{
  private readonly ILifetime p_lifetime = new Lifetime();
  private volatile bool p_disposedValue;

  protected T ToDispose<T>(T _value) where T : IDisposable => p_lifetime.ToDisposeOnEnding(_value);

  protected T ToDisposeAsync<T>(T _value) where T : IAsyncDisposable => p_lifetime.ToDisposeAsyncOnEnding(_value);

  protected T ToDisposeOnEnded<T>(T _value) where T : IDisposable => p_lifetime.ToDisposeOnEnded(_value);

  protected void ToDoOnDisposing(Action _action) => p_lifetime.DoOnEnding(_action);

  protected void ToDoOnEnded(Action _action) => p_lifetime.DoOnEnded(_action);

  protected virtual void Dispose(bool _disposing)
  {
    if (!p_disposedValue)
    {
      if (_disposing)
        p_lifetime.End();

      p_disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(_disposing: true);
    GC.SuppressFinalize(this);
  }

}
