﻿using Ax.Fw.Attributes;
using Ax.Fw.Bus;
using Ax.Fw.Interfaces;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class EBusTests
    {
        private readonly ITestOutputHelper p_output;

        public EBusTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task StressTestClientServer(int _num)
        {
            var lifetime = new Lifetime();
            try
            {
                var server = new EBusServer(lifetime, 9600);
                var client0 = new EBusClient(lifetime, new EventLoopScheduler(), 9600);
                var client1 = new EBusClient(lifetime, new EventLoopScheduler(), 9600);

                client0
                    .OfReqRes<SimpleMsgReq, SimpleMsgRes>(msg =>
                    {
                        return new SimpleMsgRes(msg.Code + 1);
                    });

                var counter = 0;
                var sw = Stopwatch.StartNew();
                Parallel.For(0, _num, _ =>
                {
                    Interlocked.Increment(ref counter);
                    var i = _;
                    var result = client1.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(i), TimeSpan.FromSeconds(3600));
                    Assert.Equal(i + 1, result?.Code);
                });
                p_output.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
                Assert.Equal(_num, counter);
                Assert.InRange(sw.ElapsedMilliseconds, 0, _num * 100);
            }
            finally
            {
                lifetime.Complete();
            }
        }


    }

    [EBusMsgAttribute]
    class SimpleMsgReq : IBusMsg
    {
        public SimpleMsgReq(int _code)
        {
            Code = _code;
        }

        public int Code { get; set; }
    }

    [EBusMsgAttribute]
    class SimpleMsgRes : IBusMsg
    {
        public SimpleMsgRes(int _code)
        {
            Code = _code;
        }

        public int Code { get; set; }
    }

}
