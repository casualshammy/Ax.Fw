using Ax.Fw.Extensions;
using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.JsonConverters;

public class SizeConverter : JsonConverter<Size>
{
  public override Size Read(
    ref Utf8JsonReader _reader,
    Type _typeToConvert,
    JsonSerializerOptions _options)
  {
    var value = _reader.GetString();
    if (value.IsNullOrEmpty())
      throw new FormatException($"Position: '{_reader.Position}': value is null or empty!");

    var split = value.Split(':');
    if (split.Length != 2)
      throw new FormatException($"Position: '{_reader.Position}': invalid segments count ({split.Length})!");

    if (!int.TryParse(split[0], out var w))
      throw new FormatException($"Position: '{_reader.Position}': invalid w segment ({split[0]})!");

    if (!int.TryParse(split[1], out var h))
      throw new FormatException($"Position: '{_reader.Position}': invalid h segment ({split[1]})!");

    return new Size(w, h);
  }

  public override void Write(
    Utf8JsonWriter _writer,
    Size _value,
    JsonSerializerOptions _options) => _writer.WriteStringValue($"{_value.Width}:{_value.Height}");
}