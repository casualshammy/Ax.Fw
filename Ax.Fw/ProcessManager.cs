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
    var newProcessSubj = _lifetime.ToDisposeOnEnded(new Subject<int>());
    var processClosedSubj = _lifetime.ToDisposeOnEnded(new Subject<int>());

    _lifetime.ToDisposeOnEnded(Pool<EventLoopScheduler>.Get(out var scanScheduler));

    Observable
      .Interval(TimeSpan.FromSeconds(1), scanScheduler)
      .ObserveOn(scanScheduler)
      .Scan(new HashSet<int>(), (_acc, _) =>
      {
        try
        {
          var snapshot = Process
            .GetProcesses()
            .ToDictionary(_ => _.Id, _ => _);

          var newAcc = snapshot
            .Select(_ => _.Key)
            .ToHashSet();

          try
          {
            if (_acc.Count == 0 && !_returnAllProcessesOnStart)
              return newAcc;

            foreach (var (processId, _) in snapshot)
              if (!_acc.Contains(processId))
                newProcessSubj.OnNext(processId);

            foreach (var processId in _acc)
              if (!snapshot.ContainsKey(processId))
                processClosedSubj.OnNext(processId);

            return newAcc;
          }
          finally
          {
            foreach (var (_, process) in snapshot)
              process.Dispose();
          }
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

  public IObservable<int> OnProcessStarted { get; }
  public IObservable<int> OnProcessClosed { get; }

}
