using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.TaskUtils;

public class TaskUtilsTests
{
  private readonly ITestOutputHelper p_output;

  public TaskUtilsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 5000)]
  public async Task WhenAllTestAsync()
  {
    var counter = 0;
    var sw = Stopwatch.StartNew();

    var task1 = Task.Run(async () =>
    {
      await Task.Delay(2000);
      Interlocked.Increment(ref counter);
      return true;
    });
    var task2 = Task.Run(async () =>
    {
      await Task.Delay(2000);
      Interlocked.Increment(ref counter);
      return Unit.Default;
    });
    var task3 = Task.Run(async () =>
    {
      await Task.Delay(2000);
      Interlocked.Increment(ref counter);
      return int.MaxValue;
    });

    var (result1, result2, result3) = await Ax.Fw.TaskUtils.WhenAll(task1, task2, task3);

    var elapsed = sw.Elapsed;
    Assert.InRange(elapsed.TotalSeconds, 1.9d, 4d);
    p_output.WriteLine($"{nameof(TaskUtilsTests)}.{nameof(WhenAllTestAsync)} | elapsed: {elapsed}");

    Assert.Equal(3, counter);
    Assert.True(result1);
    Assert.Equal(Unit.Default, result2);
    Assert.Equal(int.MaxValue, result3);
  }

  [Fact(Timeout = 5000)]
  public async Task WhenAllExceptionAsync()
  {
    var task1 = Task.Run(async () =>
    {
      await Task.Delay(2000);
      return true;
    });
    var task2 = Task.Run(async () =>
    {
      await Task.Delay(2000);
      if (DateTimeOffset.UtcNow.Year > 0)
        throw new InvalidOperationException();

      return Unit.Default;
    });

    await Assert.ThrowsAsync<InvalidOperationException>(async () => await Ax.Fw.TaskUtils.WhenAll(task1, task2));
  }

}
