#nullable enable
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using WatsonTcp;

namespace Ax.Fw.Bus
{
    public class TcpBusServer : ITcpBusServer
    {
        private readonly WatsonTcpServer p_server;
        private readonly ConcurrentDictionary<string, Unit> p_clients = new();
        private readonly Subject<(string IpPort, byte[] Data, Dictionary<object, object> Meta)> p_failedTcpMsgFlow = new();

        public TcpBusServer(ILifetime _lifetime, int _port, string _host = "127.0.0.1")
        {
            p_server = _lifetime.DisposeOnCompleted(new WatsonTcpServer(_host, _port))!;
            p_server.Events.ClientConnected += ClientConnected;
            p_server.Events.ClientDisconnected += ClientDisconnected;
            p_server.Events.MessageReceived += MessageReceived;

            async Task<bool> sendTcpMsgJob(JobContext<(string IpPort, byte[] Data, Dictionary<object, object> Meta)> _ctx)
            {
                try
                {
                    return await p_server.SendAsync(_ctx.JobInfo.Job.IpPort, _ctx.JobInfo.Job.Data, _ctx.JobInfo.Job.Meta, token: _ctx.CancellationToken);
                }
                catch
                {
                    return false;
                }
            }
            Task<PenaltyInfo> sendTcpMsgJobPenalty(JobFailContext<(string IpPort, byte[] Data, Dictionary<object, object> Meta)> _ctx)
            {
                return Task.FromResult(new PenaltyInfo(_ctx.FailedCounter < 100, TimeSpan.FromMilliseconds(_ctx.FailedCounter * 300))); // 300ms - 30 sec
            }

            WorkerTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, _lifetime, 4);

            p_server.Start();
        }

        public bool IsListening => p_server.IsListening;
        public int ClientsCount => p_clients.Count;

        private void ClientConnected(object _sender, ConnectionEventArgs _args) => p_clients.AddOrUpdate(_args.IpPort, Unit.Default, (_, _) => Unit.Default);

        private void ClientDisconnected(object _sender, DisconnectionEventArgs _args) => p_clients.TryRemove(_args.IpPort, out _);

        private void MessageReceived(object _sender, MessageReceivedEventArgs _args)
        {
            foreach (var (ipPort, _) in p_clients)
                if (ipPort != _args.IpPort)
                    if (!p_server.Send(ipPort, _args.Data, _args.Metadata))
                        p_failedTcpMsgFlow.OnNext((ipPort, _args.Data, _args.Metadata));
        }

    }
}
