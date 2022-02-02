#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers.Parts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Workers
{
    public static class AsyncTeam
    {
        /// <summary>
        /// Starts new <see cref="AsyncTeam"/>. This team will do work pieces in parallel order.
        /// </summary>
        /// <typeparam name="TJob">Object defining work data</typeparam>
        /// <param name="_jobsFlow"><see cref="IObservable{TJob}"/> of word data</param>
        /// <param name="_jobRoutine">Job main handler. This method will be executed with each <typeparam name="TJob">. Must return true if work is done successfully, false otherwise</param>
        /// <param name="_penaltyForFailedJobs">Work fails handler. Reports number of fails and exception, if exists. Must return <see cref="PenaltyInfo"/> that defines if job must be re-enqueued and delay before next attempt</param>
        /// <param name="_workers">Number of simultaneous workers</param>
        /// <returns></returns>
        public static AsyncTeam<TJob> Run<TJob>(
            IObservable<TJob> _jobsFlow,
            Func<TJob, CancellationToken, Task<bool>> _jobRoutine,
            Func<TJob, int, Exception?, CancellationToken, Task<PenaltyInfo>> _penaltyForFailedJobs,
            ILifetime _lifetime,
            int _workers,
            IScheduler? _scheduler = null)
        {
            return new AsyncTeam<TJob>(_jobsFlow, _jobRoutine, _penaltyForFailedJobs, _lifetime, _workers, _scheduler);
        }
    }

    public class AsyncTeam<TJob> : ITeam<TJob>
    {
        private readonly ConcurrentQueue<JobInfo<TJob>> p_jobQueue = new();
        private readonly List<Subject<Unit>> p_workerFlows = new();
        private readonly Func<TJob, int, Exception?, CancellationToken, Task<PenaltyInfo>> p_penaltyForFailedJobs;
        private readonly Subject<TJob> p_completedFlow;
        private readonly ILifetime p_lifetime;
        private volatile int p_failedJobs;
        private volatile int p_completedJobs;
        private volatile int p_runningJobs;

        internal AsyncTeam(
            IObservable<TJob> _jobsFlow,
            Func<TJob, CancellationToken, Task<bool>> _jobRoutineAsync,
            Func<TJob, int, Exception?, CancellationToken, Task<PenaltyInfo>> _penaltyForFailedJobsAsync,
            ILifetime _lifetime,
            int _workers,
            IScheduler? _scheduler = null)
        {
            if (_workers <= 0)
                throw new ArgumentOutOfRangeException(nameof(_workers), "Number of workers must be greater than 0");

            p_penaltyForFailedJobs = _penaltyForFailedJobsAsync;
            p_lifetime = _lifetime;

            p_completedFlow = _lifetime.DisposeOnCompleted(new Subject<TJob>());

            for (int i = 0; i < _workers; i++)
            {
                var workerFlow = _lifetime.DisposeOnCompleted(new Subject<Unit>());
                p_workerFlows.Add(workerFlow);

                var scheduler = _scheduler ?? _lifetime.DisposeOnCompleted(new EventLoopScheduler());

                workerFlow
                    .SelectAsync(async _ =>
                    {
                        while (p_jobQueue.TryDequeue(out var jobInfo))
                        {
                            Interlocked.Increment(ref p_runningJobs);
                            try
                            {
                                if (!await _jobRoutineAsync(jobInfo.Job, _lifetime.Token))
                                {
                                    await HandleFailedJobAsync(jobInfo, null);
                                    Interlocked.Increment(ref p_failedJobs);
                                }
                                else
                                {
                                    p_completedFlow.OnNext(jobInfo.Job);
                                    Interlocked.Increment(ref p_completedJobs);
                                }
                            }
                            catch (Exception ex)
                            {
                                await HandleFailedJobAsync(jobInfo, ex);
                            }
                            Interlocked.Decrement(ref p_runningJobs);
                        }
                        return Unit.Default;
                    }, scheduler)
                    .Subscribe(_lifetime.Token);
            }
            _lifetime.DoOnCompleted(() => p_workerFlows.Clear());

            _jobsFlow
                .Subscribe(x => AddNewJobToQueue(x), _lifetime.Token);
        }

        public IObservable<TJob> CompletedJobs => p_completedFlow;

        public TeamState State => new(p_runningJobs, p_jobQueue.Count, p_completedJobs, p_failedJobs);

        private void AddNewJobToQueue(TJob _job, int _failedCounter = 0)
        {
            p_jobQueue.Enqueue(new JobInfo<TJob>(_job, _failedCounter));
            foreach (var flow in p_workerFlows)
                flow.OnNext(Unit.Default);
        }

        private async Task HandleFailedJobAsync(JobInfo<TJob> _jobInfo, Exception? _ex)
        {
            var newJobInfo = new JobInfo<TJob>(_jobInfo.Job, _jobInfo.FailedCounter + 1);
            var penalty = await p_penaltyForFailedJobs(newJobInfo.Job, newJobInfo.FailedCounter, _ex, p_lifetime.Token);
            if (penalty.TryAgain && penalty.Delay != null)
                Observable
                    .Timer(penalty.Delay.Value)
                    .Subscribe(_ => AddNewJobToQueue(newJobInfo.Job, newJobInfo.FailedCounter), p_lifetime.Token);
        }

    }
}
