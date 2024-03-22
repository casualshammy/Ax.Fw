using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.JsonConverters;

public class JsonFloatAsStringConverter : JsonConverter<float>
{
  public override bool CanConvert(Type _t) => _t == typeof(float);

  public override float Read(ref Utf8JsonReader _reader, Type _typeToConvert, JsonSerializerOptions _options)
  {
    var value = _reader.GetString();
    if (float.TryParse(value, CultureInfo.InvariantCulture, out var floatValue))
      return floatValue;

    throw new Exception("Cannot unmarshal type long");
  }

  public override void Write(Utf8JsonWriter _writer, float _value, JsonSerializerOptions _options) 
    => _writer.WriteStringValue(_value.ToString(CultureInfo.InvariantCulture));

}
