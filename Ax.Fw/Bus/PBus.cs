using Ax.Fw.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Bus
{
    internal class IBusMsgSerial
    {
        public IBusMsgSerial(IBusMsg data, Guid id)
        {
            Data = data;
            Id = id;
        }

        public IBusMsg Data { get; }
        public Guid Id { get; }
    }

    public class PBus : IBus
    {
        private readonly Subject<IBusMsgSerial> p_msgFlow = new();
        private readonly ThreadPoolScheduler p_scheduler = ThreadPoolScheduler.Instance;
        private readonly ConcurrentDictionary<Type, IBusMsg> p_lastMsg = new();

        public PBus(ILifetime _lifetime)
        {
            _lifetime.DisposeOnCompleted(p_msgFlow);
        }

        public void PostMsg(IBusMsg _data)
        {
            if (_data == null)
                throw new ArgumentNullException(nameof(_data));

            PostMsg(new IBusMsgSerial(_data, Guid.NewGuid()));
        }

        private void PostMsg(IBusMsgSerial _msg)
        {
            p_lastMsg.AddOrUpdate(_msg.Data.GetType(), _msg.Data, (_, _) => _msg.Data);
            p_msgFlow.OnNext(_msg);
        }

        public IObservable<T> OfType<T>(bool includeLastValue = false) where T : IBusMsg
        {
            if (includeLastValue && p_lastMsg.TryGetValue(typeof(T), out var msg))
                return p_msgFlow
                    .ObserveOn(p_scheduler)
                    .Where(x => x.Data.GetType() == typeof(T))
                    .Select(x => x.Data)
                    .Merge(Observable.Return(msg))
                    .Cast<T>();
            else
                return p_msgFlow
                    .ObserveOn(p_scheduler)
                    .Where(x => x.Data.GetType() == typeof(T))
                    .Select(x => x.Data)
                    .Cast<T>();
        }

        public TRes PostReqRes<TReq, TRes>(TReq _req, TimeSpan _timeout)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            var mre = new ManualResetEvent(false);
            var guid = Guid.NewGuid();
            TRes result = default;
            using var subscription = p_msgFlow
                .ObserveOn(p_scheduler)
                .Where(x => x.Id == guid && x.Data.GetType() == typeof(TRes))
                .Subscribe(x =>
                {
                    result = (TRes)x.Data;
                    mre.Set();
                });
            PostMsg(new IBusMsgSerial(_req, guid));
            if (mre.WaitOne(_timeout))
                return result;

            throw new TimeoutException();
        }

        public IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            return p_msgFlow
                .ObserveOn(p_scheduler)
                .Where(x => x.Data.GetType() == typeof(TReq))
                .Subscribe(async x =>
                {
                    var guid = x.Id;
                    var result = await _func((TReq)x.Data);
                    PostMsg(new IBusMsgSerial(result, guid));
                });
        }

        public void OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg
        {
            p_msgFlow
                .ObserveOn(p_scheduler)
                .Where(x => x.Data.GetType() == typeof(TReq))
                .Subscribe(async x =>
                {
                    var guid = x.Id;
                    var result = await _func((TReq)x.Data);
                    PostMsg(new IBusMsgSerial(result, guid));
                }, _lifetime.Token);
        }

    }
}
