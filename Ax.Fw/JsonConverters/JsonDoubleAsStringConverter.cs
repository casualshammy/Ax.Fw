using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.JsonConverters;

public class JsonDoubleAsStringConverter : JsonConverter<double>
{
  public override bool CanConvert(Type _t) => _t == typeof(double);

  public override double Read(ref Utf8JsonReader _reader, Type _typeToConvert, JsonSerializerOptions _options)
  {
    var value = _reader.GetString();
    if (double.TryParse(value, CultureInfo.InvariantCulture, out var doubleValue))
      return doubleValue;

    throw new Exception("Cannot unmarshal type long");
  }

  public override void Write(Utf8JsonWriter _writer, double _value, JsonSerializerOptions _options)
    => _writer.WriteStringValue(_value.ToString(CultureInfo.InvariantCulture));

}
