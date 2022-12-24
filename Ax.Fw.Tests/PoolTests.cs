#nullable enable
using Ax.Fw.Extensions;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class PoolTests
{
    private readonly ITestOutputHelper p_output;

    public PoolTests(ITestOutputHelper output)
    {
        p_output = output;
    }

    [Fact(Timeout = 5000)]
    public async Task SimplePoolTestAsync()
    {
        var instance0 = Pool<EventLoopScheduler>.Get(out var _eventLoopScheduler0);
        var instance1 = Pool<EventLoopScheduler>.Get(out var _eventLoopScheduler1);
        try
        {
            Assert.Equal(0, Pool<EventLoopScheduler>.Count);

            instance0.Dispose();
            Assert.Equal(1, Pool<EventLoopScheduler>.Count);
            instance1.Dispose();
            Assert.Equal(2, Pool<EventLoopScheduler>.Count);

            instance0 = Pool<Stopwatch>.Get(out var _sw0);
            instance1 = Pool<Stopwatch>.Get(out var _sw1);
            Assert.Equal(0, Pool<Stopwatch>.Count);

            instance0.Dispose();
            Assert.Equal(1, Pool<Stopwatch>.Count);
            instance1.Dispose();
            Assert.Equal(2, Pool<Stopwatch>.Count);
        }
        finally
        {
            _eventLoopScheduler0.Dispose();
            _eventLoopScheduler1.Dispose();
        }
    }

}
