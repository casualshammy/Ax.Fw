using Ax.Fw.SharedTypes.Interfaces;
using System;

namespace Ax.Fw;

public abstract class DisposableStack : IDisposable
{
  private readonly ILifetime p_lifetime = new Lifetime();
  private volatile bool p_disposedValue;

  public T ToDispose<T>(T _value) where T : IDisposable
  {
    return p_lifetime.DisposeOnCompleted(_value);
  }

  public T ToDisposeAsync<T>(T _value) where T : IAsyncDisposable
  {
    return p_lifetime.DisposeAsyncOnCompleted(_value);
  }

  protected virtual void Dispose(bool _disposing)
  {
    if (!p_disposedValue)
    {
      if (_disposing)
        p_lifetime.Complete();

      p_disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(_disposing: true);
    GC.SuppressFinalize(this);
  }

}
