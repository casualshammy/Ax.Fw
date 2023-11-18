using System.Text.Json;

namespace Ax.Fw.JsonConverters;

public static class Toolkit
{
  static Toolkit()
  {
    CommonOptions = new JsonSerializerOptions()
    {
      Converters =
      {
        new ColorConverter(),
        new PointConverter(),
        new RectangleConverter(),
        new SizeConverter(),
      }
    };
    CommonOptionsIndented = new JsonSerializerOptions()
    {
      Converters =
      {
        new ColorConverter(),
        new PointConverter(),
        new RectangleConverter(),
        new SizeConverter(),
      },
      WriteIndented = true
    };
  }

  public static JsonSerializerOptions CommonOptions { get; }
  public static JsonSerializerOptions CommonOptionsIndented { get; }

}
