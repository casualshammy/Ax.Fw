using System;
using System.ComponentModel;
using System.Globalization;

namespace Ax.Fw.Converters;

public class SerializableVersionTypeConverter : TypeConverter
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
