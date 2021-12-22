#nullable enable

namespace Ax.Fw.Workers.Parts
{
    internal class JobInfo<TJob>
    {
        public JobInfo(TJob job, int failedCounter)
        {
            Job = job;
            FailedCounter = failedCounter;
        }

        public TJob Job { get; }
        public int FailedCounter { get; }

    }

}
