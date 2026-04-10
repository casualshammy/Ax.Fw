using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IReadOnlyBijection<K, V> : IEnumerable<KeyValuePair<K, V>>
  where K : notnull
  where V : notnull
{
  /// <summary>
  /// Attempts to retrieve the value associated with the specified key.
  /// </summary>
  /// <param name="_key">The key whose associated value is to be retrieved.</param>
  /// <param name="_value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise,
  /// the default value for the type of the value parameter.</param>
  /// <returns>true if the key was found and the value was retrieved successfully; otherwise, false.</returns>
  public bool TryGetByKey(K _key, [NotNullWhen(true)] out V? _value);

  /// <summary>
  /// Attempts to retrieve the key associated with the specified value.
  /// </summary>
  /// <param name="_value">The value to locate in the collection.</param>
  /// <param name="_key">When this method returns, contains the key associated with the specified value, if the value is found; otherwise,
  /// the default value for the key type.</param>
  /// <returns>true if the collection contains an entry with the specified value; otherwise, false.</returns>
  public bool TryGetByValue(V _value, [NotNullWhen(true)] out K? _key);
}