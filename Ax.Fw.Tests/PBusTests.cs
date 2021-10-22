using Ax.Fw.Bus;
using Ax.Fw.Interfaces;
using System;
using System.Diagnostics;
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
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime);
                bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(msg =>
                {
                    if (msg.Code == 1)
                        return new SimpleMsgRes(2);

                    return new SimpleMsgRes(0);
                });

                var result = bus.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(1));
                Assert.Equal(2, result?.Code);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void StressTest(int _num)
        {
            var sw = Stopwatch.StartNew();
            var lifetime = new Lifetime();
            try
            {
                var bus = new PBus(lifetime);
                bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(msg =>
                {
                    return new SimpleMsgRes(msg.Code + 1);
                });

                var counter = 0;
                Parallel.For(0, _num, _ => {
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
                bus.OfReqRes<SimpleMsgReq, SimpleMsgRes>(async msg =>
                {
                    if (msg.Code == 0)
                        await Task.Delay(5000);

                    return new SimpleMsgRes(msg.Code + 1);
                });

                var result0 = bus.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(0), TimeSpan.FromSeconds(1));
                var result1 = bus.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(1));

                Assert.Null(result0);
                Assert.Equal(2, result1?.Code);
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
