#nullable enable
using System;
using System.Threading;

namespace Ax.Fw.SharedTypes.Data.Workers
{
    public class JobFailContext<TJob>
    {
        public JobFailContext(
            TJob _job,
            int _failedCounter,
            Exception? _lastException,
            CancellationToken _cancellationToken)
        {
            Job = _job;
            FailedCounter = _failedCounter;
            LastException = _lastException;
            CancellationToken = _cancellationToken;
        }

        public TJob Job { get; }
        public int FailedCounter { get; }
        public Exception? LastException { get; }
        public CancellationToken CancellationToken { get; }
    }
}
