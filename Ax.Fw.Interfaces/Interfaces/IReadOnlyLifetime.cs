#nullable enable
using System;
using System.Threading;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IReadOnlyLifetime
    {
        CancellationToken Token { get; }
        bool CancellationRequested { get; }

        T DisposeOnCompleted<T>(T? _instance) where T : IDisposable;
        void DoOnCompleted(Action _action);
        ILifetime GetChildLifetime();
    }
}