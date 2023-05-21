#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IReadOnlyLifetime
{
    CancellationToken Token { get; }
    bool IsCancellationRequested { get; }

    /// <summary>
    /// This IObservable will produce single value (<see cref="true"/>) before completion of this instance of <see cref="IReadOnlyLifetime"/> 
    /// </summary>
    IObservable<bool> OnEnding { get; }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(parameterName: "_instance")]
    T? ToDisposeAsyncOnEnding<T>(T? _instance) where T : IAsyncDisposable;
#endif

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(parameterName: "_instance")]
#endif
    T? ToDisposeOnEnding<T>(T? _instance) where T : IDisposable;
    void DoOnEnding(Action _action);
    void DoOnEnding(Func<Task> _action);
    ILifetime? GetChildLifetime();
}