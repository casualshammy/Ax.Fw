using System;
using System.Reactive;
using System.Reactive.Concurrency;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface ILifetimeV2 : ILifetime
{
  IScheduler Scheduler { get; }

  void DoNotEndingUntilCompleted(IObservable<Unit> _observable);
  void DoNotEndUntilCompleted(IObservable<Unit> _observable);
}
