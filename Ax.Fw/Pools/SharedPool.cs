using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;

namespace Ax.Fw.Pools;

public static class SharedPool<T> where T : new()
{
  private static readonly object p_lock = new();
  private static readonly ConcurrentQueue<T> p_queue = new();
  private static long p_instanceCounter = 0;

  public static int Count => p_queue.Count;

  public static IDisposable Get(out T _instance)
  {
    lock (p_lock)
    {
      if (!p_queue.TryDequeue(out var instance))
      {
        instance = new T();
        Interlocked.Increment(ref p_instanceCounter);
      }

      _instance = instance;
      Debug.WriteLine($"{typeof(T).Name} was allocated, total available: {Count}; total allocated: {Interlocked.Read(ref p_instanceCounter)}");
      return Disposable.Create(() => Release(instance));
    }
  }

  private static void Release(T _instance)
  {
    lock (p_lock)
      p_queue.Enqueue(_instance);

    Debug.WriteLine($"{typeof(T).Name} was released, total available: {Count}; total allocated: {Interlocked.Read(ref p_instanceCounter)}");
  }
}
