using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IProcessManager
{
  IObservable<int> OnProcessStarted { get; }
  IObservable<int> OnProcessClosed { get; }
}