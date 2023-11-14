using System;
using System.Collections.Generic;

namespace Ax.Fw.Extensions;

public static class DictionaryExtensions
{
  public static int GetDictionaryHashCode<TKey, TValue>(this IDictionary<TKey, TValue?> _dictionary)
  {
    if (_dictionary.Count == 0)
      return 0;

    unchecked
    {
      int result = 17;
      foreach (var pair in _dictionary)
        result = result * 31 + (pair.Key?.GetHashCode() ?? 0) + (pair.Value?.GetHashCode() ?? 0);

      return result;
    }
  }

  public static bool DictionaryEquals<TKey, TValue>(this IDictionary<TKey, TValue?> _dictionary, IDictionary<TKey, TValue?> _anotherDictionary) where TValue : IEquatable<TValue>
  {
    if (_dictionary.Count != _anotherDictionary.Count)
      return false;

    foreach (var pair in _dictionary)
    {
      if (!_anotherDictionary.TryGetValue(pair.Key, out var value))
        return false;

      if (value == null && pair.Value == null)
        continue;

      if (value == null && pair.Value != null)
        return false;

      if (value != null && pair.Value == null)
        return false;

      if (!value!.Equals(pair.Value!))
        return false;
    }

    return true;
  }

}
