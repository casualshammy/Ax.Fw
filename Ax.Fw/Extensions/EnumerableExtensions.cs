using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions;

public static class EnumerableExtensions
{
  public static void Do<T>(this IEnumerable<T> _enumerable, Action<T> _action)
  {
    foreach (T item in _enumerable)
      _action(item);
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
