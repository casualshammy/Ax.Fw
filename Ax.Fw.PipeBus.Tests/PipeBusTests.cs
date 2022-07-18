using Ax.Fw.Extensions;
using Ax.Fw.PipeBus.Tests.Attributes;
using Ax.Fw.SharedTypes.Attributes;
using Ax.Fw.SharedTypes.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Concurrency;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Ax.Fw.PipeBus.Tests;

public class PipeBusTests
{
    private readonly ITestOutputHelper p_output;

    public PipeBusTests(ITestOutputHelper output)
    {
        p_output = output;
    }

    [Theory(Timeout = 10 * 10000)]
    [Repeat(10)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "<Pending>")]
    public async Task StressTestClientServer(int _num)
    {
        var lifetime = new AsyncLifetime();
        const string pipeName = "g4c264h3c4fhf";
        try
        {
            var server = await PipeBusServer.Run(lifetime, ThreadPoolScheduler.Instance, pipeName, false);
            var client0 = await PipeBusClient.Run(lifetime, ThreadPoolScheduler.Instance, pipeName);
            var client1 = await PipeBusClient.Run(lifetime, ThreadPoolScheduler.Instance, pipeName);

            var sendCounter = 0;
            var itemBag = new ConcurrentBag<int>();

            lifetime.DisposeOnCompleted(
                client0
                    .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
                    {
                        itemBag.Add(_msg.Code);
                        return new SimpleMsgRes(_msg.Code + 1);
                    }));

            var sw = Stopwatch.StartNew();
            var bag = new ConcurrentBag<long>();
            for (int _ = 0; _ < 1000; _++)
            {
                Interlocked.Increment(ref sendCounter);
                var i = _;
                var swi = Stopwatch.StartNew();
                var result = client1.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(i), TimeSpan.FromSeconds(5), lifetime.Token).Result;
                if (result == null)
                    p_output.WriteLine($"Error0: {i}; {itemBag.Contains(i)}");

                bag.Add(swi.ElapsedMilliseconds);
                Assert.Equal(i + 1, result?.Code);
            }
            p_output.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
            p_output.WriteLine($"Max req time: {bag.Max()}ms");
            p_output.WriteLine($"Min req time: {bag.Min()}ms");
            p_output.WriteLine($"Avg req time: {bag.Average()}ms");
            p_output.WriteLine($"Mean req time: {bag.Mean()}ms");
            Assert.Equal(1000, sendCounter);
        }
        finally
        {
            await lifetime.CompleteAsync();
            Thread.Sleep(1000);
        }
    }

    [Fact(Timeout = 5000)]
    public async Task ServerAsClientTest()
    {
        var asyncLifetime = new AsyncLifetime();
        var lifetime = new Lifetime();
        asyncLifetime.DoOnCompleted(lifetime.Complete);
        const string pipeName = "g4c264h3c4fhf";
        try
        {
            var server = await PipeBusServer.Run(asyncLifetime, ThreadPoolScheduler.Instance, pipeName, true);
            var client = await PipeBusClient.Run(asyncLifetime, ThreadPoolScheduler.Instance, pipeName);

            server.OfReqRes<SimpleMsgReq, SimpleMsgRes>(async _msg =>
            {
                await Task.Delay(100);
                return new SimpleMsgRes(_msg.Code + 100);
            }, lifetime);

            var res = await client.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(5), asyncLifetime.Token);

            Assert.Equal(101, res?.Code);
        }
        finally
        {
            await asyncLifetime.CompleteAsync();
        }
    }

}

[PipeBusMsg]
class SimpleMsgReq : IBusMsg
{
    public SimpleMsgReq(int _code)
    {
        Code = _code;
    }

    public int Code { get; set; }
}

[PipeBusMsg]
class SimpleMsgRes : IBusMsg
{
    public SimpleMsgRes(int _code)
    {
        Code = _code;
    }

    public int Code { get; set; }
}

