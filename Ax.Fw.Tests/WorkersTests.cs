#nullable enable
using Ax.Fw.Workers;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class WorkersTests
    {
        private readonly ITestOutputHelper p_output;

        public WorkersTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact(Timeout = 30000)]
        public async Task JustDoWorkTestAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var jobs = Observable
                    .Return(TimeSpan.FromMilliseconds(100))
                    .Repeat(5);

                var actuallyDoneWorkCount = 0;

                var worker = SyncTeam.Run(
                    jobs,
                    async (_job, _ct) =>
                    {
                        await Task.Delay(_job, _ct);
                        Interlocked.Increment(ref actuallyDoneWorkCount);
                        return true;
                    },
                    (_job, _fails, _ex, _ct) => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
                    lifetime);

                var list = new List<int>();
                worker.CompletedJobs
                    .ObserveOn(lifetime.DisposeOnCompleted(new EventLoopScheduler())!)
                    .Subscribe(x => list.Add(0), lifetime.Token);

                await Task.Delay(1000);
                Assert.Equal(5, actuallyDoneWorkCount);
                Assert.Equal(5, list.Count);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact(Timeout = 30000)]
        public async Task JustDoWorkMultithreadedTestAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var jobs = Observable
                    .Return(TimeSpan.FromMilliseconds(1000))
                    .Repeat(5);

                var actuallyDoneWorkCount = 0;

                var worker = AsyncTeam.Run(
                    jobs,
                    async (_job, _ct) =>
                    {
                        await Task.Delay(_job, _ct);
                        Interlocked.Increment(ref actuallyDoneWorkCount);
                        return true;
                    },
                    (_job, _fails, _ex, _ct) => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
                    lifetime,
                    5);

                var list = new List<int>();
                worker.CompletedJobs
                    .ObserveOn(lifetime.DisposeOnCompleted(new EventLoopScheduler())!)
                    .Subscribe(x => list.Add(0), lifetime.Token);

                var timeout = TimeSpan.FromMilliseconds(3000);
                while (timeout.TotalMilliseconds > 0 && (actuallyDoneWorkCount != 5 || list.Count != 5))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                    timeout = timeout.Subtract(TimeSpan.FromMilliseconds(100));
                }

                Assert.Equal(5, actuallyDoneWorkCount);
                Assert.Equal(5, list.Count);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact(Timeout = 30000)]
        public async Task NotEnoughWorkersTestAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var jobs = Observable
                    .Return(TimeSpan.FromMilliseconds(1000))
                    .Repeat(5);

                var actuallyDoneWorkCount = 0;

                var worker = AsyncTeam.Run(
                    jobs,
                    async (_job, _ct) =>
                    {
                        await Task.Delay(_job, _ct);
                        Interlocked.Increment(ref actuallyDoneWorkCount);
                        return true;
                    },
                    (_job, _fails, _ex, _ct) => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
                    lifetime,
                    4);

                var list = new List<int>();
                worker.CompletedJobs
                    .ObserveOn(lifetime.DisposeOnCompleted(new EventLoopScheduler())!)
                    .Subscribe(x => list.Add(0), lifetime.Token);

                await Task.Delay(TimeSpan.FromMilliseconds(1500));

                Assert.Equal(4, actuallyDoneWorkCount);
                Assert.Equal(4, list.Count);

                await Task.Delay(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(5, actuallyDoneWorkCount);
                Assert.Equal(5, list.Count);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact(Timeout = 30000)]
        public async Task FailedTaskTestAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var jobs = Observable
                    .Return(TimeSpan.FromMilliseconds(1000))
                    .Repeat(5);

                var actuallyDoneWorkCount = 0;

                var team = AsyncTeam.Run(
                    jobs,
                    async (_job, _ct) =>
                    {
                        await Task.Delay(_job, _ct);
                        Interlocked.Increment(ref actuallyDoneWorkCount);
                        return false;
                    },
                    (_job, _fails, _ex, _ct) => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
                    lifetime,
                    5);

                var list = new List<int>();
                team.CompletedJobs
                    .ObserveOn(lifetime.DisposeOnCompleted(new EventLoopScheduler())!)
                    .Subscribe(x => list.Add(0), lifetime.Token);

                await Task.Delay(TimeSpan.FromMilliseconds(1500));

                Assert.Equal(5, actuallyDoneWorkCount);
                Assert.Empty(list);

                Assert.Equal(5, team.State.TasksFailed);
                Assert.Equal(0, team.State.TasksRunning);
                Assert.Equal(0, team.State.TasksCompleted);
            }
            finally
            {
                if (!lifetime.Token.IsCancellationRequested)
                    lifetime.Complete();
            }
        }

        [Fact(Timeout = 30000)]
        public async Task TryOnlyOnceAndFailTestAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var jobs = Observable
                    .Return(TimeSpan.FromMilliseconds(1000))
                    .Repeat(5);

                var actuallyDoneWorkCount = 0;

                var team = AsyncTeam.Run(
                    jobs,
                    async (_job, _ct) =>
                    {
                        await Task.Delay(_job, _ct);
                        Interlocked.Increment(ref actuallyDoneWorkCount);
                        return false;
                    },
                    (_job, _fails, _ex, _ct) => Task.FromResult(new PenaltyInfo(false, null)),
                    lifetime,
                    5);

                var list = new List<int>();
                team.CompletedJobs
                    .ObserveOn(lifetime.DisposeOnCompleted(new EventLoopScheduler())!)
                    .Subscribe(x => list.Add(0), lifetime.Token);

                await Task.Delay(TimeSpan.FromMilliseconds(1500 * 2));

                Assert.Equal(5, actuallyDoneWorkCount);
                Assert.Empty(list);

                Assert.Equal(5, team.State.TasksFailed);
                Assert.Equal(0, team.State.TasksRunning);
                Assert.Equal(0, team.State.TasksCompleted);
            }
            finally
            {
                if (!lifetime.Token.IsCancellationRequested)
                    lifetime.Complete();
            }
        }

        [Fact(Timeout = 30000)]
        public async Task TryReenqueueTestAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var jobs = Observable
                    .Return(TimeSpan.FromMilliseconds(1000))
                    .Repeat(5);

                var actuallyDoneWorkCount = 0;

                var result = false;

                var team = AsyncTeam.Run(
                    jobs,
                    async (_job, _ct) =>
                    {
                        await Task.Delay(_job, _ct);
                        Interlocked.Increment(ref actuallyDoneWorkCount);
                        return result;
                    },
                    (_job, _fails, _ex, _ct) =>
                    {
                        if (actuallyDoneWorkCount >= 5)
                            result = true;

                        return Task.FromResult(new PenaltyInfo(true, TimeSpan.FromMilliseconds(1000)));
                    },
                    lifetime,
                    5);

                var list = new List<int>();
                team.CompletedJobs
                    .ObserveOn(lifetime.DisposeOnCompleted(new EventLoopScheduler())!)
                    .Subscribe(x => list.Add(0), lifetime.Token);

                await Task.Delay(TimeSpan.FromMilliseconds(1500 * 3));

                Assert.Equal(10, actuallyDoneWorkCount);
                Assert.Equal(5, list.Count);

                Assert.Equal(5, team.State.TasksFailed);
                Assert.Equal(0, team.State.TasksRunning);
                Assert.Equal(5, team.State.TasksCompleted);
            }
            finally
            {
                if (!lifetime.Token.IsCancellationRequested)
                    lifetime.Complete();
            }
        }

    }
}
