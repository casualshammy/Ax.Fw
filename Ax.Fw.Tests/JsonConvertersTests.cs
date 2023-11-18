using System.Drawing;
using System.Text.Json;
using Xunit;

namespace Ax.Fw.Tests;

public class JsonConvertersTests
{
  [Fact(Timeout = 1000)]
  public void ColorConverterTest()
  {
    var serializerOptions = new JsonSerializerOptions
    {
      Converters =
      {
        new JsonConverters.ColorConverter()
      }
    };

    var data = Color.FromArgb(147, 159, 123, 173);
    var json = JsonSerializer.Serialize(data, serializerOptions);
    var deserializedData = JsonSerializer.Deserialize<Color>(json, serializerOptions);

    Assert.NotEqual(default, deserializedData.R);
    Assert.Equal(data.R, deserializedData.R);

    Assert.NotEqual(default, deserializedData.G);
    Assert.Equal(data.G, deserializedData.G);

    Assert.NotEqual(default, deserializedData.B);
    Assert.Equal(data.B, deserializedData.B);

    Assert.NotEqual(default, deserializedData.A);
    Assert.Equal(data.A, deserializedData.A);
  }

  [Fact(Timeout = 1000)]
  public void PointConverterTest()
  {
    var serializerOptions = new JsonSerializerOptions
    {
      Converters =
      {
        new JsonConverters.PointConverter()
      }
    };

    var data = new Point(147, 159);
    var json = JsonSerializer.Serialize(data, serializerOptions);
    var deserializedData = JsonSerializer.Deserialize<Point>(json, serializerOptions);

    Assert.NotEqual(default, deserializedData.X);
    Assert.Equal(data.X, deserializedData.X);

    Assert.NotEqual(default, deserializedData.Y);
    Assert.Equal(data.Y, deserializedData.Y);
  }

  [Fact(Timeout = 1000)]
  public void RectangleConverterTest()
  {
    var serializerOptions = new JsonSerializerOptions
    {
      Converters =
      {
        new JsonConverters.RectangleConverter()
      }
    };

    var data = new Rectangle(147, 159, 123, 173);
    var json = JsonSerializer.Serialize(data, serializerOptions);
    var deserializedData = JsonSerializer.Deserialize<Rectangle>(json, serializerOptions);

    Assert.NotEqual(default, deserializedData.X);
    Assert.Equal(data.X, deserializedData.X);

    Assert.NotEqual(default, deserializedData.Y);
    Assert.Equal(data.Y, deserializedData.Y);

    Assert.NotEqual(default, deserializedData.Width);
    Assert.Equal(data.Width, deserializedData.Width);

    Assert.NotEqual(default, deserializedData.Height);
    Assert.Equal(data.Height, deserializedData.Height);
  }


  [Fact(Timeout = 1000)]
  public void SizeConverterTest()
  {
    var serializerOptions = new JsonSerializerOptions
    {
      Converters =
      {
        new JsonConverters.SizeConverter()
      }
    };

    var data = new Size(147, 159);
    var json = JsonSerializer.Serialize(data, serializerOptions);
    var deserializedData = JsonSerializer.Deserialize<Size>(json, serializerOptions);

    Assert.NotEqual(default, deserializedData.Width);
    Assert.Equal(data.Width, deserializedData.Width);

    Assert.NotEqual(default, deserializedData.Height);
    Assert.Equal(data.Height, deserializedData.Height);
  }

}
