using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Interfaces
{
    public interface IAsyncLifetime : IDisposable
    {
        CancellationToken Token { get; }
        bool CancellationRequested { get; }

        Task Complete();
        T DisposeOnCompleted<T>(T _instance) where T : IDisposable;
        void DoOnCompleted(Action _action);
        void DoOnCompleted(Func<Task> _action);
    }
}