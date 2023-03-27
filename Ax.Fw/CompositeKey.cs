using Newtonsoft.Json;
using System;
using System.Linq;

namespace Ax.Fw;

/// <summary>
/// Class for storing values like 'value0/value1/value2', i.e. composite path
/// </summary>
public class CompositeKey : IEquatable<CompositeKey>
{
  [JsonConstructor]
  public CompositeKey(
      [JsonProperty(nameof(Value))] string _value)
  {
    if (string.IsNullOrEmpty(_value) || _value.All(_x => _x == '/'))
      throw new ArgumentOutOfRangeException(nameof(_value), $"Value of '{nameof(_value)}' must be non-empty string and can't contain only '/' characters");

    Value = _value.Trim('/');
  }

  /// <summary>
  /// String representation of key
  /// </summary>
  public string Value { get; }

  /// <summary>
  /// Get parent key
  /// </summary>
  [JsonIgnore]
  public CompositeKey Parent
  {
    get
    {
      var lastIndex = Value.LastIndexOf('/');
      if (lastIndex == -1 || lastIndex == 0)
        return this;

      return new CompositeKey(Value.Substring(0, lastIndex));
    }
  }

  /// <summary>
  /// Get new composite key: {this instance} + '/' + {provided string}
  /// </summary>
  [JsonIgnore]
  public CompositeKey this[string _addPath]
  {
    get
    {
      if (_addPath == null)
        throw new ArgumentNullException(nameof(_addPath));

      return new CompositeKey($"{Value}/{_addPath.Trim('/')}");
    }
  }

  [JsonIgnore]
  public string LastPart
  {
    get
    {
      var lastIndex = Value.LastIndexOf('/');
      if (lastIndex == -1 || lastIndex == 0)
        return Value.Trim('/');

      return Value.Substring(lastIndex + 1);
    }
  }

  public override bool Equals(object? _obj) => _obj is CompositeKey key && Equals(key);

  public bool Equals(CompositeKey? _other) => _other != null && string.Equals(Value, _other.Value);

  public override int GetHashCode() => Value.GetHashCode();

  public override string ToString() => Value;

}
