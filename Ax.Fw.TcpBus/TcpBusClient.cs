#nullable enable
using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Bus;
using Ax.Fw.SharedTypes.Data.Workers;
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
using System.Reactive.Threading.Tasks;
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
        private readonly ConcurrentDictionary<Type, BusMsgSerial> p_lastMsg = new();
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

            async Task<bool> sendTcpMsgJob(JobContext<TcpMsg> _ctx)
            {
                try
                {
                    if (!p_client.Connected)
                        p_client.Connect();

                    return await p_client.SendAsync(_ctx.JobInfo.Job.JsonData, _ctx.JobInfo.Job.Meta, _ctx.CancellationToken);
                }
                catch
                {
                    return false;
                }
            }
            Task<PenaltyInfo> sendTcpMsgJobPenalty(JobFailContext<TcpMsg> _ctx)
            {
                return Task.FromResult(new PenaltyInfo(_ctx.FailedCounter < 100, TimeSpan.FromMilliseconds(_ctx.FailedCounter * 300))); // 300ms - 30 sec
            }

            WorkerTeam.Run(p_failedTcpMsgFlow, sendTcpMsgJob, sendTcpMsgJobPenalty, _lifetime, 4);

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

            var msgSerial = new BusMsgSerial(userData, guid);
            p_lastMsg[type] = msgSerial;
            p_msgFlow.OnNext(msgSerial);
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
                    .Merge(Observable.Return(msg.Data))
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
        /// Get Observable of messages by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="includeLastValue"></param>
        /// <returns></returns>
        public IObservable<BusMsgSerial> OfTypeRaw<T>(bool includeLastValue = false) where T : IBusMsg
        {
            if (includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var msg))
                return p_msgFlow
                    .Where(x => x.Data.GetType() == typeof(T))
                    .Merge(Observable.Return(msg))
                    .ObserveOn(p_scheduler);
            else
                return p_msgFlow
                    .Where(x => x.Data.GetType() == typeof(T))
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

        public void PostMsg(BusMsgSerial _msg)
        {
            p_lastMsg[_msg.Data.GetType()] = _msg;
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

        public async Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
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
