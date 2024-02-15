#nullable enable
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.TcpBus.Parts;
using Ax.Fw.Workers;
using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using WatsonTcp;

namespace Ax.Fw.TcpBus;

public static class TcpBusServerFactory
{
  public static IDisposable Create(
      int _port,
      string? _host,
      out TcpBusServer _serverInstance)
  {
    var lifetime = new Lifetime();

    if (_host == null)
      _serverInstance = new TcpBusServer(lifetime, _port);
    else
      _serverInstance = new TcpBusServer(lifetime, _port, _host);

    return Disposable.Create(lifetime.End);
  }
}

public class TcpBusServer : ITcpBusServer
{
  private readonly WatsonTcpServer p_server;
  private readonly ConcurrentDictionary<Guid, Unit> p_clients = new();
  private readonly Subject<TcpServerClientData> p_failedTcpMsgFlow = new();

  public TcpBusServer(IReadOnlyLifetime _lifetime, int _port, string _host = "127.0.0.1")
  {
    p_server = _lifetime.ToDisposeOnEnding(new WatsonTcpServer(_host, _port))!;
    p_server.Events.ClientConnected += ClientConnected;
    p_server.Events.ClientDisconnected += ClientDisconnected;
    p_server.Events.MessageReceived += MessageReceivedAsync;

    async Task<bool> sendTcpMsgJob(JobContext<TcpServerClientData, Unit> _ctx)
    {
      try
      {
        return await p_server.SendAsync(_ctx.JobInfo.Job.ClientGuid, _ctx.JobInfo.Job.Data, _ctx.JobInfo.Job.Metadata, 0, token: _ctx.CancellationToken);
      }
      catch
      {
        return false;
      }
    }
    Task<PenaltyInfo> sendTcpMsgJobPenalty(JobFailContext<TcpServerClientData> _ctx)
    {
      return Task.FromResult(new PenaltyInfo(_ctx.FailedCounter < 100, TimeSpan.FromMilliseconds(_ctx.FailedCounter * 300))); // 300ms - 30 sec
    }

    WorkerTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, _lifetime, 4);

    p_server.Start();
  }

  public bool IsListening => p_server.IsListening;
  public int ClientsCount => p_clients.Count;

  private void ClientConnected(object? _sender, ConnectionEventArgs _args) => p_clients.AddOrUpdate(_args.Client.Guid, Unit.Default, (_, _) => Unit.Default);

  private void ClientDisconnected(object? _sender, DisconnectionEventArgs _args) => p_clients.TryRemove(_args.Client.Guid, out _);

  private async void MessageReceivedAsync(object? _sender, MessageReceivedEventArgs _args)
  {
    foreach (var (guid, _) in p_clients)
      if (guid != _args.Client.Guid)
        if (!await p_server.SendAsync(guid, _args.Data, _args.Metadata))
          p_failedTcpMsgFlow.OnNext(new TcpServerClientData(guid, _args.Data, _args.Metadata));
  }

}
