using System;

namespace Ax.Fw.SharedTypes.Data.Workers;

public class JobInfo<TJob, TJobResult>
{
  public JobInfo(
    long _id,
    TJob _job,
    int _failedCounter,
    IObserver<JobResultCtx<TJobResult?>>? _completion = null)
  {
    Id = _id;
    Job = _job;
    FailedCounter = _failedCounter;
    Completion = _completion;
  }

  public long Id { get; }
  public TJob Job { get; }
  public int FailedCounter { get; }
  public IObserver<JobResultCtx<TJobResult?>>? Completion { get; }

}
