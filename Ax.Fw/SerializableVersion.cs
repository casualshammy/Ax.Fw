using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw;

[TypeConverter(typeof(TypeConverter))]
[JsonConverter(typeof(JsonConverter))]
public class SerializableVersion : IEquatable<SerializableVersion>, IComparable<SerializableVersion>
{
  public SerializableVersion(Version _version)
  {
    Major = _version.Major;
    Minor = _version.Minor;
    Build = _version.Build;
  }

  [JsonConstructor]
  public SerializableVersion(
    int major,
    int minor,
    int build)
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

  public static bool operator ==(SerializableVersion? _a, SerializableVersion? _b)
  {
    if (ReferenceEquals(_a, _b))
      return true;
    if (_a is null || _b is null)
      return false;
    return _a.Major == _b.Major && _a.Minor == _b.Minor && _a.Build == _b.Build;
  }

  public static bool operator !=(SerializableVersion a, SerializableVersion b)
  {
    return !(a == b);
  }

  public static bool operator >(SerializableVersion _a, SerializableVersion _b)
  {
    if (_a.Major < _b.Major)
      return false;
    if (_a.Major > _b.Major)
      return true;
    if (_a.Minor < _b.Minor)
      return false;
    if (_a.Minor > _b.Minor)
      return true;
    if (_a.Build < _b.Build)
      return false;
    if (_a.Build > _b.Build)
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

  public static bool operator <=(SerializableVersion _a, SerializableVersion _b)
  {
    return _a < _b || _a == _b;
  }

  public static bool operator >=(SerializableVersion _a, SerializableVersion _b)
  {
    return _a > _b || _a == _b;
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

  public int CompareTo(SerializableVersion? _other)
  {
    if (_other == null)
      return -1;

    if (this > _other)
      return 1;

    if (this < _other)
      return -1;

    return 0;
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
    public override void Write(Utf8JsonWriter _writer, SerializableVersion? _value, JsonSerializerOptions _options)
    {
      if (_value is null)
        _writer.WriteNullValue();
      else
        _writer.WriteStringValue(_value.ToString());
    }

    public override SerializableVersion? Read(
        ref Utf8JsonReader _reader,
        Type _objectType,
        JsonSerializerOptions _options)
    {
      if (_reader.TokenType == JsonTokenType.Null)
        return null;

      if (_reader.TokenType == JsonTokenType.String && _reader.GetString() is string value)
      {
        var split = value.Split('.');
        return new SerializableVersion(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
      }

      throw new BadImageFormatException("can't deserialize document id");
    }
  }

}
