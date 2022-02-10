﻿#nullable enable
using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.TcpBus.Parts;
using Ax.Fw.Workers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatsonTcp;

namespace Ax.Fw.Bus
{
    public class TcpBusClient : ITcpBus
    {
        private readonly WatsonTcpClient p_client;
        private readonly Subject<BusMsgSerial> p_msgFlow = new();
        private readonly ConcurrentDictionary<Type, IBusMsg> p_lastMsg = new();
        private readonly IScheduler p_scheduler;
        private readonly IReadOnlyDictionary<string, Type> p_typesCache;
        private readonly Subject<TcpMsg> p_failedTcpMsgFlow = new();

        public TcpBusClient(IReadOnlyLifetime _lifetime, IScheduler _scheduler, int _port)
        {
            var typesCache = new Dictionary<string, Type>();
            foreach (var type in Utilities.GetTypesWith<TcpBusMsgAttribute>(true))
                typesCache.Add(type.ToString(), type);
            p_typesCache = typesCache;

            p_scheduler = _scheduler;
            _lifetime.DisposeOnCompleted(p_msgFlow);

            p_client = _lifetime.DisposeOnCompleted(new WatsonTcpClient("127.0.0.1", _port))!;
            p_client.Events.MessageReceived += MessageReceived;
            _lifetime.DoOnCompleted(() => p_client.Events.MessageReceived -= MessageReceived);

            async Task<bool> sendTcpMsgJob(TcpMsg _msg, CancellationToken _ct)
            {
                try
                {
                    if (!p_client.Connected)
                        p_client.Connect();

                    return await p_client.SendAsync(_msg.JsonData, _msg.Meta, _ct);
                }
                catch
                {
                    return false;
                }
            }
            Task<PenaltyInfo> sendTcpMsgJobPenalty(TcpMsg _msg, int _failCount, Exception? _ex, CancellationToken _ct)
            {
                return Task.FromResult(new PenaltyInfo(_failCount < 100, TimeSpan.FromMilliseconds(_failCount * 300))); // 300ms - 30 sec
            }

            AsyncTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, _lifetime, 4, _scheduler);

            Observable
                .Interval(TimeSpan.FromSeconds(5))
                .Subscribe(_ =>
                {
                    if (!p_client.Connected)
                        p_client.Connect();
                }, _lifetime);

            if (!p_client.Connected)
                p_client.Connect();
        }

        public TcpBusClient(IReadOnlyLifetime _lifetime, int _port) : this(_lifetime, ThreadPoolScheduler.Instance, _port)
        { }

        public bool Connected => p_client.Connected;

        private void MessageReceived(object _sender, MessageReceivedEventArgs _args)
        {
            var bytes = _args.Data;
            if (bytes.Length == 0 ||
                _args.Metadata == null ||
                !_args.Metadata.TryGetValue("data_type", out var typeObject) ||
                typeObject is not string typeString ||
                !p_typesCache.TryGetValue(typeString, out var type) ||
                !_args.Metadata.TryGetValue("guid", out var guidObject) ||
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

        /// <summary>
        /// Get Observable of messages by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="includeLastValue"></param>
        /// <returns></returns>
        public IObservable<T> OfType<T>(bool includeLastValue = false) where T : IBusMsg
        {
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
            if (_data == null)
                throw new ArgumentNullException(nameof(_data));

            PostMsg(new BusMsgSerial(_data, Guid.NewGuid()));
        }

        private void PostMsg(BusMsgSerial _msg)
        {
            p_lastMsg[_msg.Data.GetType()] = _msg.Data;
            p_msgFlow.OnNext(_msg);
            var tcpMsg = new TcpMsg(
                JsonConvert.SerializeObject(_msg.Data),
                new Dictionary<object, object> { { "data_type", _msg.Data.GetType().ToString() }, { "guid", _msg.Id.ToString() } });
            if (!p_client.Send(tcpMsg.JsonData, tcpMsg.Meta))
                p_failedTcpMsgFlow.OnNext(tcpMsg);
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
