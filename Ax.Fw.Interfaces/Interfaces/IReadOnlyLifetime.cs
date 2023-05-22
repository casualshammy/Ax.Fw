using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
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
  IObservable<Unit> OnEnding { get; }

  [return: NotNullIfNotNull(parameterName: "_instance")]
  T? ToDisposeAsyncOnEnding<T>(T? _instance) where T : IAsyncDisposable;

  [return: NotNullIfNotNull(parameterName: "_instance")]
  T? ToDisposeOnEnding<T>(T? _instance) where T : IDisposable;

  void DoOnEnding(Action _action);
  void DoOnEnding(Func<Task> _action);
  ILifetime? GetChildLifetime();

  void DoNotEndingUntilCompleted(IObservable<Unit> _observable);
  void DoNotEndUntilCompleted(IObservable<Unit> _observable);
  void DoOnEnded(Action _action);
  void DoOnEnded(Func<Task> _action);
  [return: NotNullIfNotNull("_instance")]
  T? ToDisposeAsyncOnEnded<T>(T? _instance) where T : IAsyncDisposable;
  [return: NotNullIfNotNull("_instance")]
  T? ToDisposeOnEnded<T>(T? _instance) where T : IDisposable;

}