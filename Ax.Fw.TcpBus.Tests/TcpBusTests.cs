using Ax.Fw.Attributes;
using Ax.Fw.Bus;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.TcpBus.Tests.Attributes;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Ax.Fw.Tests
{
    public class TcpBusTests
    {
        private readonly ITestOutputHelper p_output;

        public TcpBusTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Theory]
        [Repeat(10)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "<Pending>")]
        public void StressTestClientServer(int _num)
        {
            var lifetime = new Lifetime();
            try
            {
                var server = new TcpBusServer(lifetime, 9600, false);
                var client0 = new TcpBusClient(lifetime, 9600);
                var client1 = new TcpBusClient(lifetime, 9600);

                var sendCounter = 0;

                lifetime.DisposeOnCompleted(
                    client0
                        .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
                        {
                            return new SimpleMsgRes(_msg.Code + 1);
                        }));

                var sw = Stopwatch.StartNew();
                var bag = new ConcurrentBag<long>();
                Parallel.For(0, 1000, _ =>
                {
                    Interlocked.Increment(ref sendCounter);
                    var i = _;
                    var swi = Stopwatch.StartNew();
                    var result = client1.PostReqResOrDefault<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(i), TimeSpan.FromSeconds(5));
                    bag.Add(swi.ElapsedMilliseconds);
                    Assert.Equal(i + 1, result?.Code);
                });
                p_output.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
                p_output.WriteLine($"Max req time: {bag.Max()}ms");
                p_output.WriteLine($"Min req time: {bag.Min()}ms");
                p_output.WriteLine($"Avg req time: {bag.Average()}ms");
                Assert.Equal(1000, sendCounter);
            }
            finally
            {
                lifetime.Complete();
            }
        }


    }

    [TcpBusMsg]
    class SimpleMsgReq : IBusMsg
    {
        public SimpleMsgReq(int _code)
        {
            Code = _code;
        }

        public int Code { get; set; }
    }

    [TcpBusMsg]
    class SimpleMsgRes : IBusMsg
    {
        public SimpleMsgRes(int _code)
        {
            Code = _code;
        }

        public int Code { get; set; }
    }

}
