using System;
using System.Collections.Generic;
using System.Linq;

namespace Ax.Fw.Extensions;

/// <summary>
/// Provides extension methods for working with dictionaries and key-value pairs.
/// </summary>
public static class DictionaryExtensions
{
  /// <summary>
  /// Calculates a hash code for the dictionary based on its keys and values.
  /// </summary>
  /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
  /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
  /// <param name="_dictionary">The dictionary for which to calculate the hash code.</param>
  /// <returns>An integer hash code representing the dictionary.</returns>
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

  /// <summary>
  /// Compares two dictionaries for equality by checking if they have the same keys and values.
  /// </summary>
  /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
  /// <typeparam name="TValue">The type of the dictionary values, which must implement <see cref="IEquatable{T}"/>.</typeparam>
  /// <param name="_dictionary">The first dictionary to compare.</param>
  /// <param name="_anotherDictionary">The second dictionary to compare.</param>
  /// <returns><c>true</c> if the dictionaries are equal; otherwise, <c>false</c>.</returns>
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

      if (value == null || pair.Value == null)
        return false;

      if (!value.Equals(pair.Value))
        return false;
    }

    return true;
  }

  /// <summary>
  /// Adds a new key-value pair to the dictionary or updates the value for an existing key using a factory function.
  /// </summary>
  /// <typeparam name="K">The type of the dictionary keys.</typeparam>
  /// <typeparam name="V">The type of the dictionary values.</typeparam>
  /// <param name="_dictionary">The dictionary to modify.</param>
  /// <param name="_key">The key to add or update.</param>
  /// <param name="_value">The value to add if the key does not exist.</param>
  /// <param name="_updateFactory">A function to compute the new value if the key already exists.</param>
  /// <returns>The new or updated value associated with the key.</returns>
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

  /// <summary>
  /// Converts a mutable dictionary to a read-only dictionary.
  /// </summary>
  /// <typeparam name="K">The type of the dictionary keys.</typeparam>
  /// <typeparam name="V">The type of the dictionary values.</typeparam>
  /// <param name="_dictionary">The dictionary to convert.</param>
  /// <returns>A read-only view of the dictionary.</returns>
  public static IReadOnlyDictionary<K, V> ToReadOnly<K, V>(this IDictionary<K, V> _dictionary)
      => (IReadOnlyDictionary<K, V>)_dictionary;

  /// <summary>
  /// Divides a range into chunks and computes the average value for each chunk based on the input key-value pairs.
  /// </summary>
  /// <param name="_values">The input key-value pairs to process.</param>
  /// <param name="_chunksCount">The number of chunks to divide the range into.</param>
  /// <param name="_start">The start of the range.</param>
  /// <param name="_end">The end of the range.</param>
  /// <returns>
  /// A read-only dictionary where the keys are the chunk boundaries and the values are the average values for each chunk.
  /// </returns>
  /// <exception cref="ArgumentException">Thrown if the range is invalid or the step size is less than 1.</exception>
  public static IReadOnlyDictionary<double, long> ComputeAverageChunks(
      this IEnumerable<KeyValuePair<long, long>> _values,
      int _chunksCount,
      long _start,
      long _end)
  {
    if (_end <= _start)
      throw new ArgumentException("The start value must be less than the end value.");

    var range = _end - _start;
    var step = (double)range / _chunksCount;
    
    if (step < 1)
      throw new ArgumentException("The step size must be at least 1.");

    // Initialize chunk boundaries and results
    var chunkBoundaries = new List<double?>();
    var chunkSums = new Dictionary<double, long>();
    var chunkCounts = new Dictionary<double, int>();

    for (int i = 0; i < _chunksCount; i++)
    {
      var boundary = _start + (i + 1) * step;
      chunkBoundaries.Add(boundary);
      chunkSums[boundary] = 0;
      chunkCounts[boundary] = 0;
    }

    // Process each value and assign it to the appropriate chunk
    foreach (var pair in _values)
    {
      var key = pair.Key;
      var value = pair.Value;

      if (key < _start)
        continue; // Skip values outside the range

      // Find the first chunk boundary that is greater than or equal to the key
      var chunkBoundary = chunkBoundaries.FirstOrDefault(_b => _b >= key);

      if (chunkBoundary == null)
        continue; // Skip values outside the range

      // Update the sum and count for the chunk
      chunkSums[chunkBoundary.Value] += value;
      chunkCounts[chunkBoundary.Value]++;
    }

    // Compute averages for each chunk
    var result = new Dictionary<double, long>();
    foreach (var boundary in chunkBoundaries)
    {
      var count = chunkCounts[boundary!.Value];
      result[boundary.Value] = count > 0 ? chunkSums[boundary.Value] / count : 0;
    }

    return result;
  }
}
