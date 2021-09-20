using System;
using System.Threading.Tasks;

namespace Ax.Fw.Interfaces
{
    public interface IPBus
    {
        IDisposable OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        void OfReqRes<TReq, TRes>(Func<TReq, Task<TRes>> _func, ILifetime _lifetime)
            where TReq : IBusMsg
            where TRes : IBusMsg;
        IObservable<T> OfType<T>(bool includeLastValue = false) where T : IBusMsg;
        void PostMsg(IBusMsg _data);
        TRes PostReqRes<TReq, TRes>(TReq _req, TimeSpan _timeout)
            where TReq : IBusMsg
            where TRes : IBusMsg;
    }
}