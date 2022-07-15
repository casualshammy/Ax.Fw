#nullable enable
using System.Threading;

namespace Ax.Fw.SharedTypes.Data.Workers
{
    public class JobContext<TJob>
    {
        public JobContext(JobInfo<TJob> _job, int _workerIndex, CancellationToken _cancellationToken)
        {
            JobInfo = _job;
            WorkerIndex = _workerIndex;
            CancellationToken = _cancellationToken;
        }

        public JobInfo<TJob> JobInfo { get; }
        public int WorkerIndex { get; }
        public CancellationToken CancellationToken { get; }
    }
}
