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

    [return: NotNullIfNotNull(parameterName: "_instance")]
    T? ToDisposeAsyncOnEnding<T>(T? _instance) where T : IAsyncDisposable;

    [return: NotNullIfNotNull(parameterName: "_instance")]
    T? ToDisposeOnEnding<T>(T? _instance) where T : IDisposable;

    void DoOnEnding(Action _action);
    void DoOnEnding(Func<Task> _action);
    ILifetime? GetChildLifetime();
}