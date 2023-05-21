#nullable enable
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.Tests.Tools;
using Ax.Fw.Workers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
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

    public WorkersTests(ITestOutputHelper _output)
    {
      p_output = _output;
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

        var worker = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(_ctx.JobInfo.Job, _ctx.CancellationToken);
              Interlocked.Increment(ref actuallyDoneWorkCount);
              return true;
            },
            _ctx => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
            lifetime,
            1);

        var list = new List<int>();
        worker.SuccessfullyCompletedJobs
            .ObserveOn(lifetime.ToDisposeOnEnding(new EventLoopScheduler())!)
            .Subscribe(_ => list.Add(0), lifetime.Token);

        await Task.Delay(1000);
        Assert.Equal(5, actuallyDoneWorkCount);
        Assert.Equal(5, list.Count);
      }
      finally
      {
        lifetime.End();
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

        var worker = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(_ctx.JobInfo.Job, _ctx.CancellationToken);
              Interlocked.Increment(ref actuallyDoneWorkCount);
              p_output.WriteLine($"Work done on worker #{_ctx.WorkerIndex}");
              return true;
            },
            _ctx => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
            lifetime,
            5);

        var list = new List<int>();
        worker.SuccessfullyCompletedJobs
            .ObserveOn(lifetime.ToDisposeOnEnding(new EventLoopScheduler())!)
            .Subscribe(_ => list.Add(0), lifetime.Token);

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
        lifetime.End();
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

        var worker = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(_ctx.JobInfo.Job, _ctx.CancellationToken);
              Interlocked.Increment(ref actuallyDoneWorkCount);
              return true;
            },
            _ctx => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
            lifetime,
            4);

        var list = new List<int>();
        worker.SuccessfullyCompletedJobs
            .ObserveOn(lifetime.ToDisposeOnEnding(new EventLoopScheduler())!)
            .Subscribe(_ => list.Add(0), lifetime.Token);

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        Assert.Equal(4, actuallyDoneWorkCount);
        Assert.Equal(4, list.Count);

        await Task.Delay(TimeSpan.FromMilliseconds(1000));

        Assert.Equal(5, actuallyDoneWorkCount);
        Assert.Equal(5, list.Count);
      }
      finally
      {
        lifetime.End();
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

        var team = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(_ctx.JobInfo.Job, _ctx.CancellationToken);
              Interlocked.Increment(ref actuallyDoneWorkCount);
              return false;
            },
            _ctx => Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1))),
            lifetime,
            5);

        var list = new List<int>();
        team.SuccessfullyCompletedJobs
            .ObserveOn(lifetime.ToDisposeOnEnding(new EventLoopScheduler())!)
            .Subscribe(_ => list.Add(0), lifetime.Token);

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
          lifetime.End();
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

        var team = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(_ctx.JobInfo.Job, _ctx.CancellationToken);
              Interlocked.Increment(ref actuallyDoneWorkCount);
              return false;
            },
            _ctx => Task.FromResult(new PenaltyInfo(false, null)),
            lifetime,
            5);

        var list = new List<int>();
        team.SuccessfullyCompletedJobs
            .ObserveOn(lifetime.ToDisposeOnEnding(new EventLoopScheduler())!)
            .Subscribe(_ => list.Add(0), lifetime.Token);

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
          lifetime.End();
      }
    }

    [Fact(Timeout = 30000)]
    public async Task TryReenqueueTestAsync()
    {
      var lifetime = new Lifetime();
      try
      {
        const int size = 10;

        var jobs = Observable
            .Return(Unit.Default)
            .Repeat(size);

        var actuallyDoneWorkCount = 0;

        var result = false;

        var team = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(TimeSpan.FromSeconds(1), _ctx.CancellationToken);
              Interlocked.Increment(ref actuallyDoneWorkCount);
              return result;
            },
            _ctx =>
            {
              if (actuallyDoneWorkCount >= size)
                result = true;

              return Task.FromResult(new PenaltyInfo(true, TimeSpan.FromSeconds(1)));
            },
            lifetime,
            size / 2);

        var sw = Stopwatch.StartNew();
        await team.SuccessfullyCompletedJobs
            .ObserveOn(lifetime.ToDisposeOnEnding(new EventLoopScheduler())!)
            .Take(size);

        //Assert.InRange(sw.ElapsedMilliseconds, 0, 4 * 1000 + 250);

        Assert.Equal(2 * size, actuallyDoneWorkCount);

        Assert.Equal(size, team.State.TasksFailed);
        Assert.Equal(0, team.State.TasksRunning);
        Assert.Equal(size, team.State.TasksCompleted);
      }
      finally
      {
        if (!lifetime.Token.IsCancellationRequested)
          lifetime.End();
      }
    }

    [Fact(Timeout = 1000)]
    public async Task WorkerIndexCheckAsync()
    {
      var lifetime = new Lifetime();
      try
      {
        const int worksCount = 8;
        const int workerCount = worksCount / 2;

        var workerIndexBag = new ConcurrentBag<int>();

        var jobs = Observable
            .Return(Unit.Default)
            .Repeat(worksCount);

        var team = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(TimeSpan.FromMilliseconds(250), _ctx.CancellationToken);
              workerIndexBag.Add(_ctx.WorkerIndex);
              return true;
            },
            _ctx => Task.FromResult(new PenaltyInfo(false, null)),
            lifetime,
            workerCount);

        await Task.Delay(TimeSpan.FromMilliseconds(750), lifetime.Token);

        Assert.Equal(worksCount, workerIndexBag.Count);
        for (int i = 0; i < workerCount; i++)
          Assert.Contains(i, workerIndexBag);
      }
      finally
      {
        lifetime.End();
      }
    }

    [Theory(Timeout = 10000)]
    [Repeat(5)]
    public async Task ShutdownTest(int _iteration)
    {
      var _ = _iteration;
      var lifetime = new Lifetime();
      try
      {
        var jobs = Observable
            .Return(Unit.Default)
            .Repeat(10000);

        var team = WorkerTeam.Run(
            jobs,
            async _ctx =>
            {
              await Task.Delay(100);
              return true;
            },
            _ctx => Task.FromResult(new PenaltyInfo(false, null)),
            lifetime,
            5);

        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.False(lifetime.IsCancellationRequested);
        lifetime.End();
        Assert.True(lifetime.IsCancellationRequested);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await team.DoWork(Unit.Default));
      }
      finally
      {
        lifetime.End();
      }
    }

  }
}
