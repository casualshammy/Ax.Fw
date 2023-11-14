using Ax.Fw.SharedTypes.Data.Bus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface ITcpBusClient
    {
        bool Connected { get; }

        IDisposable OfReqRes<TReq, TRes>(Func<TReq, TRes> _func)
            where TReq : notnull
            where TRes : notnull;
        IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
            where TReq : notnull
            where TRes : notnull;
        void OfReqRes<TReq, TRes>(Func<TReq, TRes> _func, ILifetime _lifetime)
            where TReq : notnull
            where TRes : notnull;
        void OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func, ILifetime _lifetime)
            where TReq : notnull
            where TRes : notnull;
        IObservable<T> OfType<T>(bool _includeLastValue = false) where T : notnull;
        IObservable<TcpMsg> OfTypeRaw<T>(bool _includeLastValue = false) where T : notnull;
        Task PostMsgAsync<T>(T _data, CancellationToken _ct) where T : notnull;
        Task PostMsgAsync(TcpMsg _msg, CancellationToken _ct);
        Task<TRes?> PostReqResOrDefaultAsync<TReq, TRes>(TReq _req, TimeSpan _timeout, CancellationToken _ct)
            where TReq : notnull
            where TRes : notnull;
    }




}