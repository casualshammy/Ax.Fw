using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions
{
  public static class EnumerableExtensions
  {
    public static void Do<T>(this IEnumerable<T> _enumerable, Action<T> _action)
    {
      foreach (T item in _enumerable)
        _action(item);
    }

#if !NET6_0_OR_GREATER
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> _source, Func<TSource, TKey> _keySelector)
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>();
            foreach (TSource element in _source)
            {
                if (knownKeys.Add(_keySelector(element)))
                {
                    yield return element;
                }
            }
        }
#endif

    public static async Task<T> FirstOrDefaultAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
    {
      if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
      if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
      foreach (var element in _enumerable)
      {
        if (await _predicate(element)) return element;
      }
      return default;
    }

    public static async Task<bool> AnyAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
    {
      if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
      if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
      foreach (var element in _enumerable)
      {
        if (await _predicate(element)) return true;
      }
      return false;
    }

    public static async Task<bool> AllAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
    {
      if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
      if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
      foreach (var element in _enumerable)
      {
        if (!await _predicate(element)) return false;
      }
      return true;
    }

    public static async Task<T> FirstAsync<T>(this IEnumerable<T> _enumerable, Func<T, Task<bool>> _predicate)
    {
      if (_enumerable == null) throw new ArgumentNullException(nameof(_enumerable));
      if (_predicate == null) throw new ArgumentNullException(nameof(_predicate));
      foreach (var element in _enumerable)
      {
        if (await _predicate(element)) return element;
      }
      throw new InvalidOperationException("Element not found");
    }

    public static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(this IEnumerable<TSource> _source, Func<TSource, Task<TResult>> _selector)
    {
      if (_source == null)
        throw new ArgumentNullException(nameof(_source));

      if (_selector == null)
        throw new ArgumentNullException(nameof(_selector));

      foreach (var entry in _source)
        yield return await _selector(entry);
    }

    public static async IAsyncEnumerable<TSource> WhereAsync<TSource>(this IEnumerable<TSource> _source, Func<TSource, Task<bool>> _selector)
    {
      if (_source == null)
        throw new ArgumentNullException(nameof(_source));

      if (_selector == null)
        throw new ArgumentNullException(nameof(_selector));

      foreach (var entry in _source)
        if (await _selector(entry))
          yield return entry;
    }

    public static async Task<List<TSource>> ToListAsync<TSource>(this IAsyncEnumerable<TSource> _source, CancellationToken _ct = default)
    {
      if (_source == null)
        throw new ArgumentNullException(nameof(_source));

      var list = new List<TSource>();

      await foreach (var item in _source.WithCancellation(_ct).ConfigureAwait(false))
        list.Add(item);

      return list;
    }

    public static T Mean<T>(this IEnumerable<T> _enumerable, Comparer<T>? _comparer = null)
    {
      if (_enumerable is not List<T> list)
        list = _enumerable.ToList();

      if (list.Count == 0)
        throw new ArgumentException("Enumerable is empty!", nameof(_enumerable));

      if (list.Count == 1)
        return list[0];

      list.Sort(_comparer ?? Comparer<T>.Default);
      return list[(int)Math.Floor(list.Count / 2f)];
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> _enumerable)
    {
      return _enumerable
          .Where(_x => _x != null)
          .Select(_x => _x!);
    }

    public static async Task<T[]> OrderByAsync<T>(this IEnumerable<T> _enumerable, Func<T, T, CancellationToken, Task<int>> _comparer, CancellationToken _ct = default)
    {
      var array = _enumerable.ToArray();
      if (array.Length > 1)
        await QuickSortAsync(array, 0, array.Length - 1, _comparer, _ct);

      return array;
    }

    private static async Task QuickSortAsync<T>(T[] _array, int _left, int _right, Func<T, T, CancellationToken, Task<int>> _comparer, CancellationToken _ct = default)
    {
      static void exchange(T[] _array, int _index0, int _index1)
      {
        (_array[_index1], _array[_index0]) = (_array[_index0], _array[_index1]);
      }

      int i, j;

      i = _left;
      j = _right;
      var x = _array[(_left + _right) / 2];

      while (!_ct.IsCancellationRequested)
      {
        while (await _comparer(x, _array[i], _ct) > 0)
          i++;
        while (await _comparer(x, _array[j], _ct) < 0)
          j--;

        if (i <= j)
        {
          exchange(_array, i, j);
          i++;
          j--;
        }
        if (i > j)
          break;
      }

      if (_left < j)
        await QuickSortAsync(_array, _left, j, _comparer, _ct);
      if (i < _right)
        await QuickSortAsync(_array, i, _right, _comparer, _ct);
    }

  }
}
