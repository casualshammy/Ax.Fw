using Ax.Fw.Bus;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class PBusTests
    {
        private readonly ITestOutputHelper p_output;

        public PBusTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact]
        public void TestClientServer()
        {
            p_output.WriteLine($"Starting test {nameof(TestClientServer)}...");
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime);
                lifetime.DisposeOnCompleted(bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(msg =>
                {
                    if (msg.Code == 1)
                        return new SimpleMsgRes(2);

                    return new SimpleMsgRes(0);
                }));

                var result = bus.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(1));
                Assert.Equal(2, result?.Code);
            }
            finally
            {
                lifetime.Complete();
                p_output.WriteLine($"Test {nameof(TestClientServer)} is completed");
            }
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void StressTest(int _num)
        {
            p_output.WriteLine($"Starting test {nameof(StressTest)}-{_num}...");
            var sw = Stopwatch.StartNew();
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime);
                lifetime.DisposeOnCompleted(bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(msg =>
                {
                    return new SimpleMsgRes(msg.Code + 1);
                }));

                var counter = 0;
                Parallel.For(0, _num, _ =>
                {
                    Interlocked.Increment(ref counter);
                    var i = _;
                    var result = bus.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(i), TimeSpan.FromSeconds(1));
                    Assert.Equal(i + 1, result?.Code);
                });
                //Assert.InRange(sw.ElapsedMilliseconds, 0, _num / 10);
                Assert.Equal(_num, counter);
            }
            finally
            {
                lifetime.Complete();
                p_output.WriteLine($"Test {nameof(StressTest)}-{_num} is completed");
            }
        }

        [Fact]
        public void LongRunningMsg()
        {
            var sw = Stopwatch.StartNew();
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime);
                lifetime.DisposeOnCompleted(bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(async msg =>
                {
                    p_output.WriteLine($"{sw.Elapsed} OfReqRes, Code {msg.Code}, Start");
                    if (msg.Code == 0)
                        await Task.Delay(5000);

                    p_output.WriteLine($"{sw.Elapsed} OfReqRes, Code {msg.Code}, Finish");
                    return new SimpleMsgRes(msg.Code + 1);
                }));

                var result = bus.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(1));
                Assert.Equal(2, result?.Code);

                result = bus.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(0), TimeSpan.FromSeconds(1));
                Assert.Null(result);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact]
        public async Task AsyncPostReqRes()
        {
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime);
                lifetime.DisposeOnCompleted(bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(async _msg =>
                {
                    await Task.Delay(500);
                    if (_msg.Code == 1)
                        return new SimpleMsgRes(2);

                    return new SimpleMsgRes(0);
                }));

                var result = await bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(1), lifetime.Token);
                Assert.Equal(2, result?.Code);

                result = await bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromMilliseconds(400), lifetime.Token);
                Assert.Null(result);

                var sw = Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
                await Assert.ThrowsAsync<OperationCanceledException>(() => bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(10), cts.Token));
                Assert.InRange(sw.ElapsedMilliseconds, 0, 300);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact]
        public async Task AsyncPostReqResEventLoopScheduler()
        {
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime, lifetime.DisposeOnCompleted(new EventLoopScheduler()));
                lifetime.DisposeOnCompleted(bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(async _msg =>
                {
                    await Task.Delay(500);
                    if (_msg.Code == 1)
                        return new SimpleMsgRes(2);

                    return new SimpleMsgRes(0);
                }));

                var result = await bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(1), lifetime.Token);
                Assert.Equal(2, result?.Code);

                result = await bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromMilliseconds(400), lifetime.Token);
                Assert.Null(result);

                var sw = Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
                await Assert.ThrowsAsync<OperationCanceledException>(() => bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(10), cts.Token));
                Assert.InRange(sw.ElapsedMilliseconds, 0, 300);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact]
        public async Task AsyncPostReqResLongRunningMsg()
        {
            var sw = Stopwatch.StartNew();
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime);
                lifetime.DisposeOnCompleted(bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(async msg =>
                {
                    p_output.WriteLine($"{sw.Elapsed} OfReqRes, Code {msg.Code}, Start");
                    if (msg.Code == 0)
                        await Task.Delay(5000);

                    p_output.WriteLine($"{sw.Elapsed} OfReqRes, Code {msg.Code}, Finish");
                    return new SimpleMsgRes(msg.Code + 1);
                }));

                var result = await bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(1), lifetime.Token);
                Assert.Equal(2, result?.Code);

                result = await bus.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(0), TimeSpan.FromSeconds(1), lifetime.Token);
                Assert.Null(result);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        class SimpleMsgReq : IBusMsg
        {
            public SimpleMsgReq(int _code)
            {
                Code = _code;
            }

            public int Code { get; }
        }

        class SimpleMsgRes : IBusMsg
        {
            public SimpleMsgRes(int _code)
            {
                Code = _code;
            }

            public int Code { get; }
        }

    }
}
