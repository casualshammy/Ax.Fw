#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace Ax.Fw
{
    public static class Pool<T> where T : new()
    {
        private static readonly object p_lock = new();
        private static readonly ConcurrentQueue<T> p_queue = new();
        private static readonly HashSet<T> p_allocatedInstances = new();

        public static int Count => p_queue.Count;

        public static IDisposable Get(out T _instance)
        {
            lock (p_lock)
            {
                if (!p_queue.TryDequeue(out var instance))
                {
                    instance = new T();
                    p_allocatedInstances.Add(instance);
                }

                _instance = instance;
                Debug.WriteLine($"{typeof(T).Name} was allocated, total available: {Count}; total allocated: {p_allocatedInstances.Count}");
                return Disposable.Create(() => Release(instance));
            }  
        }

        private static void Release(T _instance)
        {
            lock (p_lock)
                p_queue.Enqueue(_instance);

            Debug.WriteLine($"{typeof(T).Name} was released, total available: {Count}; total allocated: {p_allocatedInstances.Count}");
        }
    }
}
