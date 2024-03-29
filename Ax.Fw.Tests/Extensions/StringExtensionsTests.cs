using Ax.Fw.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Extensions;

public class StringExtensionsTests
{
  private readonly ITestOutputHelper p_output;

  public StringExtensionsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }


  [Fact(Timeout = 30000)]
  public void TryParseByteValueTest()
  {
    var simple = $"{1024 * 1024}";
    var kilo = "512k";
    var mega = "768M";
    var giga = "1024G";

    if (!simple.TryParseByteValue(out var simpleNumber) || simpleNumber != 1024 * 1024)
      Assert.Fail();
    if (!kilo.TryParseByteValue(out var kiloNumber) || kiloNumber != 512 * 1024)
      Assert.Fail();
    if (!mega.TryParseByteValue(out var megaNumber) || megaNumber != 768 * 1024 * 1024)
      Assert.Fail();
    if (!giga.TryParseByteValue(out var gigaNumber) || gigaNumber != (long)1024 * 1024 * 1024 * 1024)
      Assert.Fail();

    if ("not-a-number".TryParseByteValue(out _))
      Assert.Fail();
    if ("125p".TryParseByteValue(out _))
      Assert.Fail();
  }

}
