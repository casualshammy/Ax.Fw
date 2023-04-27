using System.Threading;

namespace Ax.Fw.SharedTypes.Data.Workers;

public class JobContext<TJob, TJobResult>
{
  public JobContext(JobInfo<TJob, TJobResult> _job, int _workerIndex, CancellationToken _cancellationToken)
  {
    JobInfo = _job;
    WorkerIndex = _workerIndex;
    CancellationToken = _cancellationToken;
  }

  public JobInfo<TJob, TJobResult> JobInfo { get; }
  public int WorkerIndex { get; }
  public CancellationToken CancellationToken { get; }
}
