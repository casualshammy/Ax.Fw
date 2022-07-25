#nullable enable
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class TimeWallTests
{
    private readonly ITestOutputHelper p_output;

    public TimeWallTests(ITestOutputHelper output)
    {
        p_output = output;
    }

    [Fact(Timeout = 1000)]
    public async Task SimplePoolTestAsync()
    {
        var timeWall = new TimeWall(10, TimeSpan.FromSeconds(0.25));
        for (int i = 0; i < 10; i++)
            Assert.True(timeWall.TryGetTicket());

        Assert.False(timeWall.TryGetTicket());

        await Task.Delay(500);
        for (int i = 0; i < 10; i++)
            Assert.True(timeWall.TryGetTicket());

        Assert.False(timeWall.TryGetTicket());

    }

}
