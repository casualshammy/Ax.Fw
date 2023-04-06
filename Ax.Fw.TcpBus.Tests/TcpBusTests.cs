#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.TcpBus.Tests.Attributes;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Ax.Fw.TcpBus.Tests;

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
  public void StressTestClientServer(int _)
  {
    var sendCounter = 0;
    var recCounter = 0;
    var lifetime = new Lifetime();
    try
    {
      var scheduler = lifetime.DisposeOnCompleted(new EventLoopScheduler());
      var server = new TcpBusServer(lifetime, 9600);
      var client0 = new TcpBusClient(lifetime, scheduler, 9600);
      var client1 = new TcpBusClient(lifetime, scheduler, 9600);

      Thread.Sleep(TimeSpan.FromSeconds(1));
      Assert.Equal(2, server.ClientsCount);

      lifetime.DisposeOnCompleted(
          client0
              .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
              {
                Interlocked.Increment(ref recCounter);
                return new SimpleMsgRes(_msg.Code + 1);
              }));

      var sw = Stopwatch.StartNew();
      var bag = new ConcurrentBag<long>();
      Parallel.For(0, 1000, _ =>
      {
        var i = _;
        Interlocked.Increment(ref sendCounter);
        Assert.Equal(2, server.ClientsCount);
        var swi = Stopwatch.StartNew();
        var result = client1.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(i), TimeSpan.FromSeconds(5), CancellationToken.None).Result;
        bag.Add(swi.ElapsedMilliseconds);
        Assert.Equal(i + 1, result?.Code);
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
      lifetime.Complete();
    }
  }

  [Fact]
  public async Task EncryptedTest()
  {
    var lifetime = new Lifetime();
    try
    {
      var server = new TcpBusServer(lifetime, 9600);
      var receiverClient = new TcpBusClient(lifetime, 9600, _password: "test-password");
      var senderClient = new TcpBusClient(lifetime, 9600, _password: "test-password");
      var brokenClient = new TcpBusClient(lifetime, 9600);

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

      lifetime.DisposeOnCompleted(
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
      lifetime.Complete();
    }
  }

  [Fact]
  public void EncryptedPerformanceTest()
  {
    var sendCounter = 0;
    var recCounter = 0;
    var lifetime = new Lifetime();
    try
    {
      var scheduler = lifetime.DisposeOnCompleted(new EventLoopScheduler());
      var server = new TcpBusServer(lifetime, 9600);
      var client0 = new TcpBusClient(lifetime, scheduler, 9600, _password: "test-password");
      var client1 = new TcpBusClient(lifetime, scheduler, 9600, _password: "test-password");

      Thread.Sleep(TimeSpan.FromSeconds(1));
      Assert.Equal(2, server.ClientsCount);

      lifetime.DisposeOnCompleted(
          client0
              .OfReqRes<SimpleMsgReq, SimpleMsgRes>(_msg =>
              {
                Interlocked.Increment(ref recCounter);
                return new SimpleMsgRes(_msg.Code + 1);
              }));

      var sw = Stopwatch.StartNew();
      var bag = new ConcurrentBag<long>();
      Parallel.For(0, 1000, _ =>
      {
        var i = _;
        Interlocked.Increment(ref sendCounter);
        Assert.Equal(2, server.ClientsCount);
        var swi = Stopwatch.StartNew();
        var result = client1.PostReqResOrDefaultAsync<SimpleMsgReq, SimpleMsgRes>(new SimpleMsgReq(i), TimeSpan.FromSeconds(5), CancellationToken.None).Result;
        bag.Add(swi.ElapsedMilliseconds);
        Assert.Equal(i + 1, result?.Code);
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
      lifetime.Complete();
    }
  }
}

class SimpleMsgReq : IBusMsg
{
  public SimpleMsgReq(int _code)
  {
    Code = _code;
  }

  public int Code { get; set; }
}

class SimpleMsgRes : IBusMsg
{
  public SimpleMsgRes(int _code)
  {
    Code = _code;
  }

  public int Code { get; set; }
}
