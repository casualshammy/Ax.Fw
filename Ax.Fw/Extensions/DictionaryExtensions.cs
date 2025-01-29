using System;
using System.Collections.Generic;
using System.Linq;

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

  public static IReadOnlyDictionary<long, long> AverageChunks(
    this IEnumerable<KeyValuePair<long, long>> _value,
    int _chunksCount,
    long _start,
    long _end)
  {
    var length = _end - _start;
    if (length <= 0)
      throw new ArgumentException($"Start value must be lesser than end value");

    var step = length / _chunksCount;
    if (step < 1)
      throw new ArgumentException($"Step is lesser than 1");

    var samplesCountPerChunk = new Dictionary<long, int>();
    var result = new Dictionary<long, long>();

    for (var i = 1; i <= _chunksCount; i++)
    {
      var point = _start + i * step;
      samplesCountPerChunk[point] = 0;
      result[point] = 0;
    }

    foreach (var (entryKey, entryValue) in _value)
    {
      var point = samplesCountPerChunk.Keys.First(_ => _ >= entryKey);

      // https://stackoverflow.com/questions/12636613/how-to-calculate-moving-average-without-keeping-the-count-and-data-total
      var oldAvg = result[point];
      var nextSamplesCount = samplesCountPerChunk[point] = samplesCountPerChunk[point] + 1;
      result[point] = oldAvg + (entryValue - oldAvg) / nextSamplesCount;
    }

    return result;
  }

}
