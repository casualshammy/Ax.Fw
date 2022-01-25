#nullable enable
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Ax.Fw
{
    /// <summary>
    /// Class for storing values like 'value0/value1/value2', i.e. composite path
    /// </summary>
    public class CompositeKey : IEquatable<CompositeKey>
    {
        [JsonConstructor]
        public CompositeKey(
            [JsonProperty(nameof(Value))] string _value)
        {
            if (string.IsNullOrEmpty(_value) || _value.All(x => x == '/'))
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

                return new CompositeKey(Value[..lastIndex]);
            }
        }

        /// <summary>
        /// Get new composite key: {this instance} + '/' + {provided string}
        /// </summary>
        public CompositeKey this[string _addPath]
        {
            get
            {
                if (_addPath == null)
                    throw new ArgumentNullException(nameof(_addPath));

                return new CompositeKey($"{Value}/{_addPath.Trim('/')}");
            }
        }

        public override bool Equals(object _obj)
        {
            return _obj is CompositeKey key && Equals(key);
        }

        public bool Equals(CompositeKey _other)
        {
            return string.Equals(Value, _other.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }

    }
}
