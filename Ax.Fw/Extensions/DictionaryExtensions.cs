﻿using System;
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

  public static V AddOrUpdateValue<K, V>(
    this IDictionary<K, V> _dictionary,
    K _key,
    V _value,
    Func<K, V, V> _updateFactory)
    where K : notnull
  {
    V result;
    if (_dictionary.TryGetValue(_key, out var existingValue))
      result = _updateFactory(_key, existingValue);
    else
      result = _value;

    _dictionary[_key] = result;
    return result;
  }

  public static IReadOnlyDictionary<K, V> ToReadOnly<K, V>(this IDictionary<K, V> _dictionary)
    => (IReadOnlyDictionary<K, V>)_dictionary;

}
