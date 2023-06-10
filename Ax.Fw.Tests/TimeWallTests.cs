using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class TimeWallTests
{
  private readonly ITestOutputHelper p_output;

  public TimeWallTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 30000)]
  public async Task SimpleTestAsync()
  {
    var timeWall = new TimeWall(10, TimeSpan.FromSeconds(1));
    for (int i = 0; i < 10; i++)
      Assert.True(timeWall.TryGetTicket());

    Assert.False(timeWall.TryGetTicket());

    await Task.Delay(TimeSpan.FromSeconds(2));

    for (int i = 0; i < 10; i++)
      Assert.True(timeWall.TryGetTicket());

    Assert.False(timeWall.TryGetTicket());

  }

}
