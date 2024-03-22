using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.JsonConverters;

public class JsonLongAsStringConverter : JsonConverter<long>
{
  public override bool CanConvert(Type _t) => _t == typeof(long);

  public override long Read(ref Utf8JsonReader _reader, Type _typeToConvert, JsonSerializerOptions _options)
  {
    var value = _reader.GetString();
    if (long.TryParse(value, CultureInfo.InvariantCulture, out var longValue))
      return longValue;

    throw new Exception("Cannot unmarshal type long");
  }

  public override void Write(Utf8JsonWriter _writer, long _value, JsonSerializerOptions _options) 
    => _writer.WriteStringValue(_value.ToString(CultureInfo.InvariantCulture));

}
