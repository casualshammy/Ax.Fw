using Ax.Fw.Converters;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Ax.Fw;

[TypeConverter(typeof(SerializableVersionTypeConverter))]
[JsonConverter(typeof(SerializableVersionJsonConverter))]
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

}
