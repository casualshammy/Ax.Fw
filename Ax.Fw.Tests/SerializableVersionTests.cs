using System.Text.Json;
using Xunit;

namespace Ax.Fw.Tests;

public class SerializableVersionTests
{
  [Fact]
  public void SerializationTest()
  {
    var version0 = new SerializableVersion(1,2,3);
    var json = JsonSerializer.Serialize(version0);
    Assert.Equal("\"1.2.3\"", json);
    var version1 = JsonSerializer.Deserialize<SerializableVersion>(json);
    Assert.Equal(1, version1?.Major);
    Assert.Equal(2, version1?.Minor);
    Assert.Equal(3, version1?.Build);
  }
}
