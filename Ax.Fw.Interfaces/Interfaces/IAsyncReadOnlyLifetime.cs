#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IAsyncReadOnlyLifetime
    {
        CancellationToken Token { get; }
        bool CancellationRequested { get; }

#if NETSTANDARD2_1_OR_GREATER
        [return: NotNullIfNotNull("_instance")]
        T? DisposeAsyncOnCompleted<T>(T? _instance) where T : IAsyncDisposable;
#endif

#if NETSTANDARD2_1_OR_GREATER
        [return: NotNullIfNotNull(parameterName: "_instance")]
#endif
        T? DisposeOnCompleted<T>(T? _instance) where T : IDisposable;
        void DoOnCompleted(Action _action);
        void DoOnCompleted(Func<Task> _action);
        IAsyncLifetime GetChildLifetime();
    }

}