#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IReadOnlyLifetime
    {
        CancellationToken Token { get; }
        bool CancellationRequested { get; }

#if NETSTANDARD2_1_OR_GREATER
        [return: NotNullIfNotNull(parameterName: "_instance")]
#endif
        T? DisposeOnCompleted<T>(T? _instance) where T : IDisposable;
        void DoOnCompleted(Action _action);
        ILifetime GetChildLifetime();
    }
}