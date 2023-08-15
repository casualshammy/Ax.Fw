using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IProcessManager
{
  IObservable<ProcessEventData> OnProcessStarted { get; }
  IObservable<ProcessEventData> OnProcessClosed { get; }
}