using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions;

public static class ObjectExtensions
{
  /// <summary>
  /// Breadth-first search
  /// </summary>
  public static IEnumerable<T> BFS<T>(
    this T _this,
    Func<T, IEnumerable<T>> _children,
    bool _skipRoot = false)
  {
    var visited = new HashSet<T>();
    var q = new Queue<T>();

    visited.Add(_this);

    if (!_skipRoot)
      q.Enqueue(_this);
    else
    {
      var children = _children(_this);
      if (children != null)
      {
        foreach (var child in children)
        {
          q.Enqueue(child);
          visited.Add(child);
        }
      }
    }

    while (q.Count > 0)
    {
      var i = q.Dequeue();
      yield return i;
      var children = _children(i);
      if (children == null)
        continue;

      foreach (var child in children)
      {
        if (visited.Contains(child))
          continue;

        q.Enqueue(child);
        visited.Add(child);
      }
    }
  }

  /// <summary>
  /// Breadth-first search
  /// </summary>
  public static async IAsyncEnumerable<T> BFSAsync<T>(
    this T _this,
    Func<T, CancellationToken, Task<IEnumerable<T>>> _children,
    bool _skipRoot = false,
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    var visited = new HashSet<T>();
    var q = new Queue<T>();

    visited.Add(_this);

    if (!_skipRoot)
      q.Enqueue(_this);
    else
    {
      var children = await _children(_this, _ct).ConfigureAwait(false);
      if (children != null)
      {
        foreach (var child in children)
        {
          q.Enqueue(child);
          visited.Add(child);
        }
      }
    }

    while (q.Count > 0)
    {
      var i = q.Dequeue();
      yield return i;

      var children = await _children(i, _ct).ConfigureAwait(false);
      if (children == null)
        continue;

      foreach (var child in children)
      {
        if (visited.Contains(child))
          continue;

        q.Enqueue(child);
        visited.Add(child);
      }
    }
  }

  public unsafe static byte[] MarshalToByteArray<T>(this T _struct)
    where T : struct
  {
    var size = Marshal.SizeOf(_struct);
    var result = new byte[size];

    var ptr = IntPtr.Zero;
    try
    {
      ptr = Marshal.AllocHGlobal(size);
      Marshal.StructureToPtr(_struct, ptr, true);
      Marshal.Copy(ptr, result, 0, size);
    }
    finally
    {
      Marshal.FreeHGlobal(ptr);
    }

    return result;
  }

}
