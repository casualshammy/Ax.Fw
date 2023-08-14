using System;
using System.Diagnostics;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IProcessManager
{
  IObservable<Process> OnProcessStarted { get; }
  IObservable<int> OnProcessClosed { get; }
}