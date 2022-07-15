#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
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
    public static class WorkerTeam
    {
        /// <summary>
        /// Starts new <see cref="WorkerTeam"/>. This team will do work in parallel order.
        /// </summary>
        /// <typeparam name="TJob">Object defining work data</typeparam>
        /// <param name="_jobsFlow"><see cref="IObservable{TJob}"/> of word data</param>
        /// <param name="_jobRoutine">Job main handler. This method will be executed for each <typeparam name="TJob">. Must return true if work is done successfully, false otherwise</param>
        /// <param name="_penaltyForFailedJobs">Work fails handler. Reports number of fails and exception, if exists. Must return <see cref="PenaltyInfo"/> that defines if job must be re-enqueued and delay before next attempt</param>
        /// <param name="_workers">Number of simultaneous workers</param>
        /// <returns></returns>
        public static WorkerTeam<TJob, Unit> Run<TJob>(
            IObservable<TJob> _jobsFlow,
            Func<JobContext<TJob>, Task<bool>> _jobRoutine,
            Func<JobFailContext<TJob>, Task<PenaltyInfo>> _penaltyForFailedJobs,
            IReadOnlyLifetime _lifetime,
            int _workers)
        {
            return new WorkerTeam<TJob, Unit>(_jobsFlow, async _ctx =>
            {
                var result = await _jobRoutine(_ctx);
                if (result)
                    return Unit.Default;

                throw new InvalidOperationException($"Job failed");
            }, _penaltyForFailedJobs, _lifetime, _workers);
        }

        // <summary>
        /// Starts new <see cref="WorkerTeam"/>. This team will do work in parallel order.
        /// </summary>
        /// <typeparam name="TJob">Object defining work data</typeparam>
        /// <param name="_jobsFlow"><see cref="IObservable{TJob}"/> of word data</param>
        /// <param name="_jobRoutine">Job main handler. This method will be executed for each <typeparam name="TJob">. Must return nullable value if work is done successfully, or throw any exception otherwise</param>
        /// <param name="_penaltyForFailedJobs">Work fails handler. Reports number of fails and exception, if exists. Must return <see cref="PenaltyInfo"/> that defines if job must be re-enqueued and delay before next attempt</param>
        /// <param name="_workers">Number of simultaneous workers</param>
        /// <returns></returns>
        public static WorkerTeam<TJob, TJobResult> Run<TJob, TJobResult>(
            IObservable<TJob> _jobsFlow,
            Func<JobContext<TJob>, Task<TJobResult?>> _jobRoutineAsync,
            Func<JobFailContext<TJob>, Task<PenaltyInfo>> _penaltyForFailedJobsAsync,
            IReadOnlyLifetime _lifetime,
            int _workers)
        {
            return new WorkerTeam<TJob, TJobResult>(_jobsFlow, _jobRoutineAsync, _penaltyForFailedJobsAsync, _lifetime, _workers);
        }
    }

    public class WorkerTeam<TJob, TJobResult> : ITeam<TJob, TJobResult>
    {
        private readonly Func<JobFailContext<TJob>, Task<PenaltyInfo>> p_penaltyForFailedJobs;
        private readonly IReadOnlyLifetime p_lifetime;
        private readonly Subject<JobResultCtx<TJobResult?>> p_completedFlow;
        private readonly List<Subject<Unit>> p_workerFlows = new();
        private readonly ConcurrentQueue<JobInfo<TJob>> p_jobQueue = new();
        private volatile int p_failedJobs;
        private volatile int p_completedJobs;
        private volatile int p_runningJobs;
        private long p_taskIdCounter = -1;

        internal WorkerTeam(
            IObservable<TJob> _jobsFlow,
            Func<JobContext<TJob>, Task<TJobResult?>> _jobRoutineAsync,
            Func<JobFailContext<TJob>, Task<PenaltyInfo>> _penaltyForFailedJobsAsync,
            IReadOnlyLifetime _lifetime,
            int _workers)
        {
            if (_workers <= 0)
                throw new ArgumentOutOfRangeException(nameof(_workers), "Number of workers must be greater than 0");

            p_penaltyForFailedJobs = _penaltyForFailedJobsAsync;
            p_lifetime = _lifetime;

            p_completedFlow = _lifetime.DisposeOnCompleted(new Subject<JobResultCtx<TJobResult?>>())!;

            for (int i = 0; i < _workers; i++)
            {
                var workerFlow = _lifetime.DisposeOnCompleted(new Subject<Unit>())!;
                p_workerFlows.Add(workerFlow);

                var scheduler = _lifetime.DisposeOnCompleted(new EventLoopScheduler())!;

                var index = i;

                workerFlow
                    .SelectAsync(async _ =>
                    {
                        while (p_jobQueue.TryDequeue(out var jobInfo))
                        {
                            Interlocked.Increment(ref p_runningJobs);
                            try
                            {
                                var ctx = new JobContext<TJob>(jobInfo, index, _lifetime.Token);
                                var result = await _jobRoutineAsync(ctx);
                                Interlocked.Increment(ref p_completedJobs);
                                p_completedFlow.OnNext(new JobResultCtx<TJobResult?>(result, jobInfo.Id));
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref p_failedJobs);
                                await HandleFailedJobAsync(jobInfo, ex);
                            }
                            Interlocked.Decrement(ref p_runningJobs);
                        }
                    }, scheduler)
                    .Subscribe(_lifetime);
            }

            var addScheduler = _lifetime.DisposeOnCompleted(new EventLoopScheduler())!;
            _jobsFlow
                .ObserveOn(addScheduler)
                .Subscribe(_x => AddNewJobToQueue(new JobInfo<TJob>(Interlocked.Increment(ref p_taskIdCounter), _x, 0)), _lifetime);
        }

        public IObservable<JobResultCtx<TJobResult?>> CompletedJobs => p_completedFlow;

        public TeamState State => new(p_runningJobs, p_jobQueue.Count, p_completedJobs, p_failedJobs);

        public async Task<TJobResult?> DoWork(TJob _job)
        {
            if (!p_lifetime.CancellationRequested)
            {
                var taskId = Interlocked.Increment(ref p_taskIdCounter);
                AddNewJobToQueue(new JobInfo<TJob>(taskId, _job, 0));
                var entry = await p_completedFlow.FirstOrDefaultAsync(_x => _x.JobIndex == taskId);
                return entry.Result;
            }

            throw new InvalidOperationException($"This instance of '{nameof(WorkerTeam<TJob, TJobResult>)}' is already completed");
        }

        public void PostWork(TJob _job)
        {
            if (!p_lifetime.CancellationRequested)
                AddNewJobToQueue(new JobInfo<TJob>(Interlocked.Increment(ref p_taskIdCounter), _job, 0));
        }

        private void AddNewJobToQueue(JobInfo<TJob> _jobInfo)
        {
            p_jobQueue.Enqueue(_jobInfo);
            foreach (var flow in p_workerFlows)
                if (!p_lifetime.CancellationRequested)
                    flow.OnNext();
        }

        private async Task HandleFailedJobAsync(JobInfo<TJob> _jobInfo, Exception? _ex)
        {
            var newJobInfo = new JobInfo<TJob>(_jobInfo.Id, _jobInfo.Job, _jobInfo.FailedCounter + 1);
            var penalty = await p_penaltyForFailedJobs(new JobFailContext<TJob>(newJobInfo.Job, newJobInfo.FailedCounter, _ex, p_lifetime.Token));
            if (penalty.TryAgain && penalty.Delay != null)
                Observable
                    .Timer(penalty.Delay.Value)
                    .Subscribe(_ => AddNewJobToQueue(newJobInfo), p_lifetime);
        }

    }

}
