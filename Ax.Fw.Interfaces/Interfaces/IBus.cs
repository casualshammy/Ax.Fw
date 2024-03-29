﻿#nullable enable
using Ax.Fw.SharedTypes.Data.Bus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IBus
    {
        IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        void OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        IDisposable OfReqRes<TReq, TRes>(Func<TReq, TRes> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        void OfReqRes<TReq, TRes>(Func<TReq, TRes> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        IObservable<T> OfType<T>(bool includeLastValue = false) where T : IBusMsg;
        void PostMsg(IBusMsg _data);
        TRes PostReqRes<TReq, TRes>(TReq _req, TimeSpan _timeout)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        TRes? PostReqResOrDefault<TReq, TRes>(TReq _req, TimeSpan _timeout)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
            where TReq : IBusMsg
            where TRes : IBusMsg;
    }

    public interface IPipeBus
    {
        bool Connected { get; }

        IDisposable OfReqRes<TReq, TRes>(Func<TReq, TRes> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        void OfReqRes<TReq, TRes>(Func<TReq, TRes> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        void OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        IObservable<T> OfType<T>(bool includeLastValue = false) where T : IBusMsg;
        IObservable<BusMsgSerial> OfTypeRaw<T>(bool includeLastValue = false) where T : IBusMsg;
        Task PostMsg(IBusMsg _data);
        Task PostMsg(BusMsgSerial _msg);
        Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
            where TReq : IBusMsg
            where TRes : IBusMsg;
    }




}