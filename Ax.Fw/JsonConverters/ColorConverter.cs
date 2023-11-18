using Ax.Fw.Extensions;
using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.JsonConverters;

public class ColorConverter : JsonConverter<Color>
{
  public override Color Read(
    ref Utf8JsonReader _reader,
    Type _typeToConvert,
    JsonSerializerOptions _options)
  {
    var value = _reader.GetString();
    if (value.IsNullOrEmpty())
      throw new FormatException($"Position: '{_reader.Position}': value is null or empty!");

    var split = value.Split(':');
    if (split.Length != 4)
      throw new FormatException($"Position: '{_reader.Position}': invalid segments count ({split.Length})!");

    if (!byte.TryParse(split[0], out var r))
      throw new FormatException($"Position: '{_reader.Position}': invalid r segment ({split[0]})!");

    if (!byte.TryParse(split[1], out var g))
      throw new FormatException($"Position: '{_reader.Position}': invalid g segment ({split[1]})!");

    if (!byte.TryParse(split[2], out var b))
      throw new FormatException($"Position: '{_reader.Position}': invalid b segment ({split[2]})!");

    if (!byte.TryParse(split[3], out var a))
      throw new FormatException($"Position: '{_reader.Position}': invalid a segment ({split[3]})!");

    return Color.FromArgb(a, r, g, b);
  }

  public override void Write(
    Utf8JsonWriter _writer,
    Color _value,
    JsonSerializerOptions _options) => _writer.WriteStringValue($"{_value.R}:{_value.G}:{_value.B}:{_value.A}");
}
