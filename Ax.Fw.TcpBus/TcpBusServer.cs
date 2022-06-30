#nullable enable
using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.TcpBus.Parts;
using Ax.Fw.Workers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatsonTcp;

namespace Ax.Fw.Bus
{
    public class TcpBusServer : ITcpBus
    {
        private readonly WatsonTcpServer p_server;
        private ImmutableHashSet<string> p_clients = ImmutableHashSet<string>.Empty;
        private readonly Subject<BusMsgSerial> p_msgFlow = new();
        private readonly ConcurrentDictionary<Type, IBusMsg> p_lastMsg = new();
        private readonly IScheduler p_scheduler;
        private readonly IReadOnlyDictionary<string, Type> p_typesCache;
        private readonly bool p_includeClient;
        private readonly Subject<(string IpPort, byte[] Data, Dictionary<object, object> Meta)> p_failedTcpMsgFlow = new();

        public TcpBusServer(ILifetime _lifetime, IScheduler _scheduler, int _port, bool _includeClient)
        {
            var typesCache = new Dictionary<string, Type>();
            foreach (var type in Utilities.GetTypesWith<TcpBusMsgAttribute>(true))
                typesCache.Add(type.ToString(), type);
            p_typesCache = typesCache;

            p_scheduler = _scheduler;
            _lifetime.DisposeOnCompleted(p_msgFlow);
            p_includeClient = _includeClient;

            p_server = _lifetime.DisposeOnCompleted(new WatsonTcpServer("127.0.0.1", _port))!;
            p_server.Events.ClientConnected += ClientConnected;
            p_server.Events.ClientDisconnected += ClientDisconnected;
            p_server.Events.MessageReceived += MessageReceived;

            async Task<bool> sendTcpMsgJob((string IpPort, byte[] Data, Dictionary<object, object> Meta) _msg, CancellationToken _ct)
            {
                try
                {
                    return await p_server.SendAsync(_msg.IpPort, _msg.Data, _msg.Meta, token: _ct);
                }
                catch
                {
                    return false;
                }
            }
            Task<PenaltyInfo> sendTcpMsgJobPenalty((string IpPort, byte[] Data, Dictionary<object, object> Meta) _msg, int _failCount, Exception? _ex, CancellationToken _ct)
            {
                return Task.FromResult(new PenaltyInfo(_failCount < 100, TimeSpan.FromMilliseconds(_failCount * 300))); // 300ms - 30 sec
            }

            WorkerTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, _lifetime, 4, new EventLoopScheduler());

            p_server.Start();
        }

        public TcpBusServer(ILifetime _lifetime, int _port, bool _includeClient) : this(_lifetime, ThreadPoolScheduler.Instance, _port, _includeClient)
        { }

        public bool Connected => p_server.IsListening;

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
            if (p_includeClient)
            {
                var bytes = args.Data;
                if (bytes.Length == 0 ||
                    args.Metadata == null ||
                    !args.Metadata.TryGetValue("data_type", out var typeObject) ||
                    typeObject is not string typeString ||
                    !p_typesCache.TryGetValue(typeString, out var type) ||
                    !args.Metadata.TryGetValue("guid", out var guidObject) ||
                    guidObject is not string guidString ||
                    !Guid.TryParse(guidString, out var guid))
                    return;

                var json = Encoding.UTF8.GetString(bytes);
                if (json == null)
                    return;

                if (JsonConvert.DeserializeObject(json, type) is not IBusMsg userData)
                    return;

                p_lastMsg[type] = userData;
                p_msgFlow.OnNext(new BusMsgSerial(userData, guid));
            }

            foreach (var ipPort in p_clients)
                if (ipPort != args.IpPort)
                    if (!p_server.Send(ipPort, args.Data, args.Metadata))
                        p_failedTcpMsgFlow.OnNext((ipPort, args.Data, args.Metadata));
        }


        /// <summary>
        /// Get Observable of messages by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="includeLastValue"></param>
        /// <returns></returns>
        public IObservable<T> OfType<T>(bool includeLastValue = false) where T : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            if (includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var msg))
                return p_msgFlow
                    .Where(x => x.Data.GetType() == typeof(T))
                    .Select(x => x.Data)
                    .Merge(Observable.Return(msg))
                    .Cast<T>()
                    .ObserveOn(p_scheduler);
            else
                return p_msgFlow
                    .Where(x => x.Data.GetType() == typeof(T))
                    .Select(x => x.Data)
                    .Cast<T>()
                    .ObserveOn(p_scheduler);
        }

        /// <summary>
        /// Send message to bus
        /// </summary>
        /// <param name="_data"></param>
        public void PostMsg(IBusMsg _data)
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            if (_data == null)
                throw new ArgumentNullException(nameof(_data));

            PostMsg(new BusMsgSerial(_data, Guid.NewGuid()));
        }

        private void PostMsg(BusMsgSerial _msg)
        {
            p_lastMsg[_msg.Data.GetType()] = _msg.Data;
            p_msgFlow.OnNext(_msg);
            var data = JsonConvert.SerializeObject(_msg.Data);
            var meta = new Dictionary<object, object> { { "data_type", _msg.Data.GetType().ToString() }, { "guid", _msg.Id.ToString() } };
            foreach (var ipPort in p_clients)
                p_server.Send(ipPort, data, meta);
        }

        /// <summary>
        /// Send message and wait for answer
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TRes"></typeparam>
        /// <param name="_req"></param>
        /// <param name="_timeout"></param>
        /// <returns></returns>
        public TRes? PostReqResOrDefault<TReq, TRes>(TReq _req, TimeSpan _timeout)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            var mre = new ManualResetEvent(false);
            var guid = Guid.NewGuid();
            TRes? result = default;
            using var subscription = p_msgFlow
                .Where(x => x.Id == guid && x.Data.GetType() == typeof(TRes))
                .ObserveOn(p_scheduler)
                .Subscribe(x =>
                {
                    result = (TRes)x.Data;
                    mre.Set();
                });
            PostMsg(new BusMsgSerial(_req, guid));
            if (mre.WaitOne(_timeout))
                return result;

            return default;
        }

        public async Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            var guid = Guid.NewGuid();
            var ignoredGuid = Guid.NewGuid();

            //var value = await Observable
            //    .Merge(
            //        p_msgFlow.Where(_x => _x.Id == guid && _x.Data.GetType() == typeof(TRes)),
            //        Observable.Timer(_timeout).Select(_ => new BusMsgSerial(new EmptyBusMsg(), Guid.Empty)),
            //        Observable.Timer(TimeSpan.Zero).Select(_ =>
            //        {
            //            PostMsg(new BusMsgSerial(_req, guid));
            //            return new BusMsgSerial(new EmptyBusMsg(), ignoredGuid);
            //        }))
            //    .ObserveOn(p_scheduler)
            //    .FirstOrDefaultAsync(_x => _x.Id != ignoredGuid);

            try
            {
                var value = await TaskObservableExtensions.ToTask(Observable
                    .Merge(
                        p_msgFlow.Where(_x => _x.Id == guid && _x.Data.GetType() == typeof(TRes)),
                        Observable.Timer(_timeout).Select(_ => new BusMsgSerial(new EmptyBusMsg(), Guid.Empty)),
                        Observable.Timer(TimeSpan.Zero).Select(_ =>
                        {
                            PostMsg(new BusMsgSerial(_req, guid));
                            return new BusMsgSerial(new EmptyBusMsg(), ignoredGuid);
                        }))
                    .ObserveOn(p_scheduler)
                    .FirstOrDefaultAsync(_x => _x.Id != ignoredGuid), _ct);

                if (value != default && value.Id != Guid.Empty)
                    return (TRes)value.Data;

                return default;
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException(_ct);
            }
        }

        /// <summary>
        /// Send message and wait for answer
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TRes"></typeparam>
        /// <param name="_req"></param>
        /// <param name="_timeout"></param>
        /// <returns></returns>
        public TRes PostReqRes<TReq, TRes>(TReq _req, TimeSpan _timeout)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            var result = PostReqResOrDefault<TReq, TRes>(_req, _timeout);
            return result ?? throw new TimeoutException();
        }

        /// <summary>
        /// Create a handler of messages of specific type ('server')
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TRes"></typeparam>
        /// <param name="_func"></param>
        /// <returns></returns>
        public IDisposable OfReqRes<TReq, TRes>(Func<TReq, TRes> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            return p_msgFlow
                .Where(x => x.Data.GetType() == typeof(TReq))
                .ObserveOn(p_scheduler)
                .Subscribe(x =>
                {
                    var guid = x.Id;
                    var result = _func((TReq)x.Data);

                    PostMsg(new BusMsgSerial(result, guid));
                });
        }

        /// <summary>
        /// Create a handler of messages of specific type ('server')
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TRes"></typeparam>
        /// <param name="_func"></param>
        /// <returns></returns>
        public IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            return p_msgFlow
                .Where(_x => _x.Data.GetType() == typeof(TReq))
                .ObserveOn(p_scheduler)
                .SelectAsync(async _x =>
                {
                    var guid = _x!.Id;
                    var result = await _func((TReq)_x.Data);

                    PostMsg(new BusMsgSerial(result, guid));
                    return Unit.Default;
                })
                .Subscribe();
        }

        /// <summary>
        /// Create a handler of messages of specific type ('server')
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TRes"></typeparam>
        /// <param name="_func"></param>
        /// <param name="_lifetime"></param>
        public void OfReqRes<TReq, TRes>(Func<TReq, TRes> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            p_msgFlow
                .Where(x => x.Data.GetType() == typeof(TReq))
                .ObserveOn(p_scheduler)
                .Subscribe(x =>
                {
                    var guid = x.Id;
                    var result = _func((TReq)x.Data);

                    PostMsg(new BusMsgSerial(result, guid));
                }, _lifetime.Token);
        }

        /// <summary>
        /// Create a handler of messages of specific type ('server')
        /// </summary>
        /// <typeparam name="TReq"></typeparam>
        /// <typeparam name="TRes"></typeparam>
        /// <param name="_func"></param>
        /// <param name="_lifetime"></param>
        public void OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            if (!p_includeClient)
                throw new InvalidOperationException($"Client features is not enabled! See constructor parameters.");

            p_msgFlow
                .Where(_x => _x.Data.GetType() == typeof(TReq))
                .ObserveOn(p_scheduler)
                .SelectAsync(async _x =>
                {
                    var guid = _x!.Id;
                    var result = await _func((TReq)_x.Data);

                    PostMsg(new BusMsgSerial(result, guid));
                    return Unit.Default;
                })
                .Subscribe(_lifetime.Token);
        }

    }
}
