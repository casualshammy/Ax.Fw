#nullable enable
using Ax.Fw.Bus.Parts;
using Ax.Fw.Interfaces;
using H.Formatters;
using H.Pipes;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Text;
using WatsonTcp;

namespace Ax.Fw.Bus
{
    public class EBusServer
    {
        private readonly WatsonTcpServer p_server;
        private readonly IScheduler p_scheduler;
        private readonly ConcurrentDictionary<Type, EBusMsg> p_lastServerMsg = new();
        private ImmutableHashSet<string> p_clients = ImmutableHashSet<string>.Empty;

        public EBusServer(ILifetime _lifetime, IScheduler _scheduler, int _port)
        {
            p_scheduler = _scheduler;

            p_server = _lifetime.DisposeOnCompleted(new WatsonTcpServer("127.0.0.1", _port));
            p_server.Events.ClientConnected += ClientConnected;
            p_server.Events.ClientDisconnected += ClientDisconnected;
            p_server.Events.MessageReceived += MessageReceived;
            p_server.Start();
        }

        private void ClientConnected(object sender, ConnectionEventArgs args)
        {
            p_clients = p_clients.Add(args.IpPort);
        }

        private void ClientDisconnected(object sender, DisconnectionEventArgs args)
        {
            p_clients = p_clients.Remove(args.IpPort);
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            foreach (var ipPort in p_clients)
                if (ipPort != args.IpPort)
                    p_server.Send(ipPort, args.Data, args.Metadata);
        }


    }
}
