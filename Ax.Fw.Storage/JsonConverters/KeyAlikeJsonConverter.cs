using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Storage.JsonConverters;

public class KeyAlikeJsonConverter : JsonConverter<KeyAlike>
{
  public override KeyAlike Read(
    ref Utf8JsonReader _reader,
    Type _typeToConvert,
    JsonSerializerOptions _options)
  {
    var value = _reader.GetString();
    if (value == null)
      throw new FormatException($"Position: '{_reader.Position}': value is null!");

    return new KeyAlike(value);
  }

  public override void Write(
    Utf8JsonWriter _writer,
    KeyAlike _value,
    JsonSerializerOptions _options) => _writer.WriteStringValue(_value.Key);

}