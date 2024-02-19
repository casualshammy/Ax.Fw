using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Converters;

public class SerializableVersionJsonConverter : JsonConverter<SerializableVersion>
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
