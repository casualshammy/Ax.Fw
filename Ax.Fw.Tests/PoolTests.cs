#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.Pools;
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
    public void SimplePoolTest()
    {
        var instance0 = SharedPool<EventLoopScheduler>.Get(out var _eventLoopScheduler0);
        var instance1 = SharedPool<EventLoopScheduler>.Get(out var _eventLoopScheduler1);
        try
        {
            Assert.Equal(0, SharedPool<EventLoopScheduler>.Count);

            instance0.Dispose();
            Assert.Equal(1, SharedPool<EventLoopScheduler>.Count);
            instance1.Dispose();
            Assert.Equal(2, SharedPool<EventLoopScheduler>.Count);

            instance0 = SharedPool<Stopwatch>.Get(out var _sw0);
            instance1 = SharedPool<Stopwatch>.Get(out var _sw1);
            Assert.Equal(0, SharedPool<Stopwatch>.Count);

            instance0.Dispose();
            Assert.Equal(1, SharedPool<Stopwatch>.Count);
            instance1.Dispose();
            Assert.Equal(2, SharedPool<Stopwatch>.Count);
        }
        finally
        {
            _eventLoopScheduler0.Dispose();
            _eventLoopScheduler1.Dispose();
        }
    }

}
