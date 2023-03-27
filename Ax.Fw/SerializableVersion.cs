using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Ax.Fw;

[TypeConverter(typeof(TypeConverter))]
[JsonConverter(typeof(JsonConverter))]
public class SerializableVersion : IEquatable<SerializableVersion>
{
  public SerializableVersion(Version version)
  {
    Major = version.Major;
    Minor = version.Minor;
    Build = version.Build;
  }

  [JsonConstructor]
  public SerializableVersion(
      [JsonProperty(nameof(Major))] int major,
      [JsonProperty(nameof(Minor))] int minor,
      [JsonProperty(nameof(Build))] int build)
  {
    Major = major;
    Minor = minor;
    Build = build;
  }

  public SerializableVersion()
  {
    Major = 0;
    Minor = 0;
    Build = 0;
  }

  public int Major { get; }

  public int Minor { get; }

  public int Build { get; }

  public static bool operator ==(SerializableVersion a, SerializableVersion b)
  {
    if (ReferenceEquals(a, b))
      return true;
    if (a is null || b is null)
      return false;
    return a.Major == b.Major && a.Minor == b.Minor && a.Build == b.Build;
  }

  public static bool operator !=(SerializableVersion a, SerializableVersion b)
  {
    return !(a == b);
  }

  public static bool operator >(SerializableVersion a, SerializableVersion b)
  {
    if (a.Major < b.Major)
      return false;
    if (a.Major > b.Major)
      return true;
    if (a.Minor < b.Minor)
      return false;
    if (a.Minor > b.Minor)
      return true;
    if (a.Build < b.Build)
      return false;
    if (a.Build > b.Build)
      return true;
    return false;
  }

  public static bool operator <(SerializableVersion a, SerializableVersion b)
  {
    if (a.Major < b.Major)
      return true;
    if (a.Major > b.Major)
      return false;
    if (a.Minor < b.Minor)
      return true;
    if (a.Minor > b.Minor)
      return false;
    if (a.Build < b.Build)
      return true;
    if (a.Build > b.Build)
      return false;
    return false;
  }

  public static bool operator <=(SerializableVersion a, SerializableVersion b)
  {
    return a < b || a == b;
  }

  public static bool operator >=(SerializableVersion a, SerializableVersion b)
  {
    return a > b || a == b;
  }

  public bool Equals(SerializableVersion? _other)
  {
    return 
      _other is not null && 
      Major == _other.Major && 
      Minor == _other.Minor && 
      Build == _other.Build;
  }

  public override bool Equals(object? _obj)
  {
    if (_obj is null) return false;
    if (ReferenceEquals(this, _obj)) return true;
    return _obj.GetType() == GetType() && Equals((SerializableVersion)_obj);
  }

  public override int GetHashCode()
  {
    return Major ^ Minor ^ Build;
  }

  public override string ToString()
  {
    return Major + "." + Minor + "." + Build;
  }


  class TypeConverter : System.ComponentModel.TypeConverter
  {
    public override bool CanConvertFrom(ITypeDescriptorContext? _context, Type _sourceType)
        => _sourceType == typeof(string);

    public override bool CanConvertTo(ITypeDescriptorContext? _context, Type? _destinationType)
        => _destinationType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext? _context, CultureInfo? _culture, object _value)
    {
      if (_value is string sv)
      {
        var split = sv.Split('.');
        return new SerializableVersion(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
      }

      throw new InvalidOperationException();
    }

    public override object ConvertTo(ITypeDescriptorContext? _context, CultureInfo? _culture, object? _value, Type _destinationType)
    {
      if (_value == null)
        throw new InvalidOperationException("Can't convert from null!");

      if (_destinationType == typeof(string))
        return ((SerializableVersion)_value).ToString();

      throw new InvalidOperationException();
    }
  }

  class JsonConverter : JsonConverter<SerializableVersion>
  {
    public override void WriteJson(JsonWriter _writer, SerializableVersion? _value, JsonSerializer _serializer)
    {
      if (_value is null)
        _writer.WriteNull();
      else
        _writer.WriteValue(_value.ToString());
    }

    public override SerializableVersion? ReadJson(
        JsonReader _reader,
        Type _objectType,
        SerializableVersion? _existingValue,
        bool _hasExistingValue,
        JsonSerializer _serializer)
    {
      if (_reader.TokenType == JsonToken.Null)
        return null;

      if (_reader.TokenType == JsonToken.String && _reader.Value is string value)
      {
        var split = value.Split('.');
        return new SerializableVersion(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
      }

      throw new BadImageFormatException("can't deserialize document id");
    }
  }

}
