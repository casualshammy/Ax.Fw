using System;
using System.Threading;

namespace Ax.Fw.Interfaces
{
    public interface ILifetime
    {
        CancellationToken Token { get; }
        bool CancellationRequested { get; }

        void Complete();
        T DisposeOnCompleted<T>(T _instance) where T : IDisposable;
        void DoOnCompleted(Action _action);
    }
}