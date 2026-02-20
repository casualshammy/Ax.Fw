using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ax.Fw.Collections;

/// <summary>
/// Represents a thread-safe, bidirectional, one-to-one mapping between keys and values, allowing efficient lookup in both
/// directions.
/// </summary>
/// <remarks>A concurrent bijection ensures that each key is associated with exactly one value, and each value is associated
/// with exactly one key. Lookups and removals can be performed by either key or value. Setting a key-value pair will
/// replace any existing mapping for the specified key or value. This class is thread-safe.</remarks>
/// <typeparam name="K">The type of the keys in the bijection. Each key must be unique and not null.</typeparam>
/// <typeparam name="V">The type of the values in the bijection. Each value must be unique and not null.</typeparam>
public class ConcurrentBijection<K, V> : IEnumerable<KeyValuePair<K, V>>
  where K : notnull
  where V : notnull
{
  private readonly Dictionary<K, V> p_forward = [];
  private readonly Dictionary<V, K> p_reverse = [];
  private readonly object p_lock = new();

  /// <summary>
  /// Associates the specified key with the specified value, updating existing mappings as necessary to maintain a
  /// one-to-one relationship between keys and values.
  /// </summary>
  /// <remarks>If the key or value is already present in the mapping, their previous associations are removed to
  /// ensure that each key maps to a unique value and each value maps to a unique key. This method overwrites any
  /// existing mapping for the specified key or value.</remarks>
  /// <param name="_key">The key to associate with the specified value.</param>
  /// <param name="_value">The value to associate with the specified key.</param>
  public void Set(K _key, V _value)
  {
    lock (p_lock)
    {
      if (p_forward.TryGetValue(_key, out var existingValue))
        p_reverse.Remove(existingValue);

      if (p_reverse.TryGetValue(_value, out var existingKey))
        p_forward.Remove(existingKey);

      p_forward[_key] = _value;
      p_reverse[_value] = _key;
    }
  }

  /// <summary>
  /// Attempts to retrieve the value associated with the specified key.
  /// </summary>
  /// <param name="_key">The key whose associated value is to be retrieved.</param>
  /// <param name="_value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise,
  /// the default value for the type of the value parameter.</param>
  /// <returns>true if the key was found and the value was retrieved successfully; otherwise, false.</returns>
  public bool TryGetByKey(K _key, [NotNullWhen(true)] out V _value)
  {
    lock (p_lock)
    {
      if (!p_forward.TryGetValue(_key, out var value))
      {
        _value = default!;
        return false;
      }

      _value = value;
      return true;
    }
  }

  /// <summary>
  /// Attempts to retrieve the key associated with the specified value.
  /// </summary>
  /// <param name="_value">The value to locate in the collection.</param>
  /// <param name="_key">When this method returns, contains the key associated with the specified value, if the value is found; otherwise,
  /// the default value for the key type.</param>
  /// <returns>true if the collection contains an entry with the specified value; otherwise, false.</returns>
  public bool TryGetByValue(V _value, [NotNullWhen(true)] out K _key)
  {
    lock (p_lock)
    {
      if (!p_reverse.TryGetValue(_value, out var key))
      {
        _key = default!;
        return false;
      }

      _key = key;
      return true;
    }
  }

  /// <summary>
  /// Removes the element with the specified key from the collection.
  /// </summary>
  /// <param name="_key">The key of the element to remove.</param>
  /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
  public bool RemoveByKey(K _key)
  {
    lock (p_lock)
    {
      if (!p_forward.TryGetValue(_key, out var value))
        return false;

      p_forward.Remove(_key);
      p_reverse.Remove(value);
      return true;
    }
  }

  /// <summary>
  /// Removes the entry with the specified value from the collection.
  /// </summary>
  /// <param name="_value">The value of the entry to remove.</param>
  /// <returns>true if the entry was found and removed; otherwise, false.</returns>
  public bool RemoveByValue(V _value)
  {
    lock (p_lock)
    {
      if (!p_reverse.TryGetValue(_value, out var key))
        return false;

      p_reverse.Remove(_value);
      p_forward.Remove(key);
      return true;
    }
  }

  /// <summary>
  /// Returns an enumerator that iterates through the collection of key/value pairs.
  /// </summary>
  /// <returns>An enumerator that can be used to iterate through the collection of key/value pairs.</returns>
  public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
  {
    lock (p_lock)
      return p_forward.ToArray().AsEnumerable().GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
    => GetEnumerator();
}
