using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IAsyncReadOnlyLifetime
    {
        CancellationToken Token { get; }
        bool CancellationRequested { get; }

        T DisposeOnCompleted<T>(T _instance) where T : IDisposable;
        void DoOnCompleted(Action _action);
        void DoOnCompleted(Func<Task> _action);
        IAsyncLifetime GetChildLifetime();
    }

}