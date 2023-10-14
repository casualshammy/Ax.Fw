using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;

namespace Ax.Fw.Pools;

public class Pool<T>
{
  private readonly object p_lock = new();
  private readonly ConcurrentQueue<T> p_queue = new();
  private readonly Func<T> p_factory;
  private readonly Action<T>? p_onRelease;
  private long p_instanceCounter = 0;

  public Pool(Func<T> _factory, Action<T>? _onRelease)
  {
    p_factory = _factory;
    p_onRelease = _onRelease;
  }

  public int Count => p_queue.Count;

  public IDisposable Get(out T _instance)
  {
    lock (p_lock)
    {
      if (!p_queue.TryDequeue(out var instance))
      {
        instance = p_factory();
        Interlocked.Increment(ref p_instanceCounter);
      }

      _instance = instance;
      Debug.WriteLine($"{typeof(T).Name} was allocated, total available: {Count}; total allocated: {Interlocked.Read(ref p_instanceCounter)}");
      return Disposable.Create(() => Release(instance));
    }
  }

  private void Release(T _instance)
  {
    lock (p_lock)
    {
      p_onRelease?.Invoke(_instance);
      p_queue.Enqueue(_instance);
    }

    Debug.WriteLine($"{typeof(T).Name} was released, total available: {Count}; total allocated: {Interlocked.Read(ref p_instanceCounter)}");
  }

}