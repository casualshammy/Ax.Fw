using System;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Extensions;

public class DateTimeOffsetExtensionsTests
{
  private readonly ITestOutputHelper p_output;

  public DateTimeOffsetExtensionsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 30000)]
  public void ToHumanFriendlyStringTest()
  {
    var now = DateTimeOffset.Now;

    var dateTime0 = now - TimeSpan.FromSeconds(36);
    var srt0 = dateTime0.ToHumanFriendlyString(DateTimeOffsetToHumanFriendlyStringOptions.Default);
    Assert.Equal($"36 sec", srt0);

    var dateTime1 = now - TimeSpan.FromMinutes(36);
    var srt1 = dateTime1.ToHumanFriendlyString(DateTimeOffsetToHumanFriendlyStringOptions.Default);
    Assert.Equal($"36 min", srt1);

    var dateTime2 = now - TimeSpan.FromHours(14);
    var srt2 = dateTime2.ToHumanFriendlyString(DateTimeOffsetToHumanFriendlyStringOptions.Default);
    Assert.Equal(dateTime2.ToString(DateTimeOffsetToHumanFriendlyStringOptions.Default.FallbackFormat), srt2);

    var dateTime3 = now - TimeSpan.FromHours(36);
    var srt3 = dateTime3.ToHumanFriendlyString(DateTimeOffsetToHumanFriendlyStringOptions.Default);
    Assert.Equal(dateTime3.ToString(DateTimeOffsetToHumanFriendlyStringOptions.Default.FallbackFormat), srt3);

    var dateTime4 = now - TimeSpan.FromSeconds(14 * 3600 + 17 * 60 + 48);
    var srt4 = dateTime4.ToHumanFriendlyString(DateTimeOffsetToHumanFriendlyStringOptions.Default);
    Assert.Equal(dateTime4.ToString(DateTimeOffsetToHumanFriendlyStringOptions.Default.FallbackFormat), srt4);

  }

}
