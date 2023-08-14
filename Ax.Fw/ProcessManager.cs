using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ax.Fw;

public class ProcessManager : IProcessManager
{
  public ProcessManager(
    IReadOnlyLifetime _lifetime,
    bool _returnAllProcessesOnStart = false,
    Action<Exception>? _onError = null)
  {
    var newProcessSubj = _lifetime.ToDisposeOnEnded(new Subject<Process>());
    var processClosedSubj = _lifetime.ToDisposeOnEnded(new Subject<int>());

    _lifetime.ToDisposeOnEnded(Pool<EventLoopScheduler>.Get(out var scanScheduler));

    Observable
      .Interval(TimeSpan.FromSeconds(1), scanScheduler)
      .ObserveOn(scanScheduler)
      .Scan(new Dictionary<int, Process>(), (_acc, _) =>
      {
        try
        {
          var snapshot = Process
            .GetProcesses()
            .ToDictionary(_ => _.Id, _ => _);

          if (_acc.Count == 0 && !_returnAllProcessesOnStart)
            return snapshot;

          foreach (var (processId, process) in snapshot)
            if (!_acc.ContainsKey(processId))
              newProcessSubj.OnNext(process);

          foreach (var (processId, process) in _acc)
          {
            if (!snapshot.ContainsKey(processId))
              processClosedSubj.OnNext(processId);

            process.Dispose();
          }

          return snapshot;
        }
        catch (Exception ex)
        {
          _onError?.Invoke(ex);
          return _acc;
        }
      })
      .Subscribe(_lifetime);

    _lifetime.ToDisposeOnEnded(Pool<EventLoopScheduler>.Get(out var clientScheduler));

    OnProcessStarted = newProcessSubj.ObserveOn(clientScheduler);
    OnProcessClosed = processClosedSubj.ObserveOn(clientScheduler);
  }

  public IObservable<Process> OnProcessStarted { get; }
  public IObservable<int> OnProcessClosed { get; }

}
