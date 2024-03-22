using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.JsonConverters;

public class JsonIntAsStringConverter : JsonConverter<int>
{
  public override bool CanConvert(Type _t) => _t == typeof(int);

  public override int Read(ref Utf8JsonReader _reader, Type _typeToConvert, JsonSerializerOptions _options)
  {
    var value = _reader.GetString();
    if (int.TryParse(value, CultureInfo.InvariantCulture, out var intValue))
      return intValue;

    throw new Exception("Cannot unmarshal type int");
  }

  public override void Write(Utf8JsonWriter _writer, int _value, JsonSerializerOptions _options) 
    => _writer.WriteStringValue(_value.ToString(CultureInfo.InvariantCulture));

}
