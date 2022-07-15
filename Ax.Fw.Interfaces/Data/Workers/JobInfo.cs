#nullable enable

namespace Ax.Fw.SharedTypes.Data.Workers
{
    public class JobInfo<TJob>
    {
        public JobInfo(long _id, TJob _job, int _failedCounter)
        {
            Id = _id;
            Job = _job;
            FailedCounter = _failedCounter;
        }

        public long Id { get; }
        public TJob Job { get; }
        public int FailedCounter { get; }

    }

}
