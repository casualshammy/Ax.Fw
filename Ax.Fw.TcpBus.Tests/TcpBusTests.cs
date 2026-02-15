#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Attributes;
using Ax.Fw.TcpBus.Tests.Attributes;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Ax.Fw.TcpBus.Tests;

[JsonSerializable(typeof(SimpleMsgReq))]
[JsonSerializable(typeof(SimpleMsgRes))]
internal partial class CustomTcpBusJsonCtx : JsonSerializerContext { }

public class TcpBusTests
{
  private static SemaphoreSlim p_semaphore = new(1, 1);
  private static readonly Type[] p_types =
  [
    typeof(SimpleMsgReq),
    typeof(SimpleMsgRes)
  ];

  private readonly ITestOutputHelper p_output;

  public TcpBusTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Theory]
  [Repeat(10)]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "<Pending>")]
  public async Task StressTestClientServer(int _)
  {
    await p_semaphore.WaitAsync();
    try
    {
      var sendCounter = 0;
      var recCounter = 0;
      var lifetime = new Lifetime();
      try
      {
        var scheduler = lifetime.ToDisposeOnEnding(new EventLoopScheduler());
        var server = new TcpBusServer(lifetime, 9600);
        var client0 = new TcpBusClient(lifetime, scheduler, p_types, CustomTcpBusJsonCtx.Default, 9600);
        var client1 = new TcpBusClient(lifetime, scheduler, p_types, CustomTcpBusJsonCtx.Default, 9600);

        Thread.Sleep(TimeSpan.FromSeconds(1));
        Assert.Equal(2, server.ClientsCount);

        lifetime.ToDisposeOnEnding(
            client0
                .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
                {
                  Interlocked.Increment(ref recCounter);
                  return new SimpleMsgRes(_msg.Code + 1);
                }));

        var sw = Stopwatch.StartNew();
        var bag = new ConcurrentBag<long>();
        var intEnumerable = Enumerable.Range(0, 1000);
        await Parallel.ForEachAsync(intEnumerable, lifetime.Token, async (_i, _ct) =>
        {
          Interlocked.Increment(ref sendCounter);
          Assert.Equal(2, server.ClientsCount);
          var swi = Stopwatch.StartNew();
          var result = await client1.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(_i), TimeSpan.FromSeconds(5), CancellationToken.None);
          bag.Add(swi.ElapsedMilliseconds);
          Assert.Equal(_i + 1, result?.Code);
        });
        p_output.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
        p_output.WriteLine($"Max req time: {bag.Max()}ms");
        p_output.WriteLine($"Min req time: {bag.Min()}ms");
        p_output.WriteLine($"Avg req time: {bag.Average()}ms");
        p_output.WriteLine($"Mean req time: {bag.Mean()}ms");
        Assert.Equal(1000, sendCounter);
      }
      catch
      {
        p_output.WriteLine($"Sent: {sendCounter}");
        p_output.WriteLine($"Received: {recCounter}");
        throw;
      }
      finally
      {
        lifetime.End();
      }
    }
    finally
    {
      p_semaphore.Release();
    }
  }

  [Fact]
  public async Task EncryptedTest()
  {
    await p_semaphore.WaitAsync();
    try
    {
      var lifetime = new Lifetime();
      try
      {
        var server = new TcpBusServer(lifetime, 9600);
        var receiverClient = new TcpBusClient(lifetime, p_types, CustomTcpBusJsonCtx.Default, 9600, _password: "test-password");
        var senderClient = new TcpBusClient(lifetime, p_types, CustomTcpBusJsonCtx.Default, 9600, _password: "test-password");
        var brokenClient = new TcpBusClient(lifetime, p_types, CustomTcpBusJsonCtx.Default, 9600);

        Thread.Sleep(TimeSpan.FromSeconds(1));
        Assert.Equal(3, server.ClientsCount);

        var receiverClientSubs = receiverClient
            .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
            {
              return new SimpleMsgRes(_msg.Code + 1);
            });

        var result = await senderClient.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(5), lifetime.Token);
        Assert.Equal(2, result?.Code);

        receiverClientSubs.Dispose();

        lifetime.ToDisposeOnEnding(
            brokenClient
                .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
                {
                  return new SimpleMsgRes(_msg.Code + 1);
                }));

        var brokenResult = await senderClient.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(1), TimeSpan.FromSeconds(5), lifetime.Token);
        Assert.Null(brokenResult);
      }
      finally
      {
        lifetime.End();
      }
    }
    finally
    {
      p_semaphore.Release();
    }
  }

  [Fact]
  public async Task EncryptedPerformanceTest()
  {
    await p_semaphore.WaitAsync();
    try
    {
      var sendCounter = 0;
      var recCounter = 0;
      var lifetime = new Lifetime();
      try
      {
        var scheduler = lifetime.ToDisposeOnEnding(new EventLoopScheduler());
        var server = new TcpBusServer(lifetime, 9600);
        var client0 = new TcpBusClient(lifetime, scheduler, p_types, CustomTcpBusJsonCtx.Default, 9600, _password: "test-password");
        var client1 = new TcpBusClient(lifetime, scheduler, p_types, CustomTcpBusJsonCtx.Default, 9600, _password: "test-password");

        Thread.Sleep(TimeSpan.FromSeconds(1));
        Assert.Equal(2, server.ClientsCount);

        lifetime.ToDisposeOnEnding(
            client0
                .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
                {
                  Interlocked.Increment(ref recCounter);
                  return new SimpleMsgRes(_msg.Code + 1);
                }));

        var sw = Stopwatch.StartNew();
        var bag = new ConcurrentBag<long>();
        var intEnumerable = Enumerable.Range(0, 1000);
        await Parallel.ForEachAsync(intEnumerable, lifetime.Token, async (_i, _ct) =>
        {
          Interlocked.Increment(ref sendCounter);
          Assert.Equal(2, server.ClientsCount);
          var swi = Stopwatch.StartNew();
          var result = await client1.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(_i), TimeSpan.FromSeconds(5), _ct);
          bag.Add(swi.ElapsedMilliseconds);
          Assert.Equal(_i + 1, result?.Code);
        });
        p_output.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
        p_output.WriteLine($"Max req time: {bag.Max()}ms");
        p_output.WriteLine($"Min req time: {bag.Min()}ms");
        p_output.WriteLine($"Avg req time: {bag.Average()}ms");
        p_output.WriteLine($"Mean req time: {bag.Mean()}ms");
        Assert.Equal(1000, sendCounter);
      }
      catch
      {
        p_output.WriteLine($"Sent: {sendCounter}");
        p_output.WriteLine($"Received: {recCounter}");
        throw;
      }
      finally
      {
        lifetime.End();
      }
    }
    finally
    {
      p_semaphore.Release();
    }
  }

}

[TcpMsgType("test-simple-msg-req")]
record SimpleMsgReq(int Code);

[TcpMsgType("test-simple-msg-res")]
record SimpleMsgRes(int Code);
