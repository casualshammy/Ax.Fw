#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IReadOnlyLifetime
{
    CancellationToken Token { get; }
    bool CancellationRequested { get; }

    /// <summary>
    /// This IObservable will produce single value (<see cref="true"/>) before completion of this instance of <see cref="IReadOnlyLifetime"/> 
    /// </summary>
    IObservable<bool> OnCompleteStarted { get; }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(parameterName: "_instance")]
    T? DisposeAsyncOnCompleted<T>(T? _instance) where T : IAsyncDisposable;
#endif

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(parameterName: "_instance")]
#endif
    T? DisposeOnCompleted<T>(T? _instance) where T : IDisposable;
    void DoOnCompleted(Action _action);
    void DoOnCompleted(Func<Task> _action);
    ILifetime GetChildLifetime();
}