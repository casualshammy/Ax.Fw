using Ax.Fw.Extensions;
using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.JsonConverters;

public class PointConverter : JsonConverter<Point>
{
  public override Point Read(
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

    if (!int.TryParse(split[0], out var x))
      throw new FormatException($"Position: '{_reader.Position}': invalid x segment ({split[0]})!");

    if (!int.TryParse(split[1], out var y))
      throw new FormatException($"Position: '{_reader.Position}': invalid y segment ({split[1]})!");

    return new Point(x, y);
  }

  public override void Write(
    Utf8JsonWriter _writer,
    Point _value,
    JsonSerializerOptions _options) => _writer.WriteStringValue($"{_value.X}:{_value.Y}");
}
