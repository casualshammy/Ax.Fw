using System;
using System.Collections.Generic;
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

  [Fact]
  public void ToHumanFriendlyStringTest()
  {
    var now = DateTimeOffset.Now;
    var reference = new Dictionary<DateTimeOffset, string>()
    {
      {now - new TimeSpan(25, 0, 0), $"{(now - new TimeSpan(25, 0, 0)):yyyy/MM/dd HH:mm:ss}"},
      {now - new TimeSpan(10, 11, 12), $"10 hours 11 min 12 sec" },
      {now - new TimeSpan(10, 11, 00), $"10 hours 11 min 0 sec" },
      {now - new TimeSpan(10, 0, 12), $"10 hours 0 min 12 sec" },
      {now - new TimeSpan(10, 0, 0), $"10 hours 0 min 0 sec" },
      {now - new TimeSpan(0, 11, 12), $"11 min 12 sec" },
      {now - new TimeSpan(0, 11, 00), $"11 min 0 sec" },
      {now - new TimeSpan(0, 00, 12), $"12 sec" },
      {now - new TimeSpan(0, 0, 0), $"0 sec" },
    };

    foreach (var (dateTime, refResult) in reference)
    {
      var result = dateTime.ToHumanFriendlyString(DateTimeOffsetToHumanFriendlyStringOptions.Default with { MaxUnit = DateTimeOffsetToHumanFriendlyStringMaxUnit.Hours });
      Assert.Equal(refResult, result);
    }
  }

}
