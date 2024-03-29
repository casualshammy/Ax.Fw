﻿using Ax.Fw.Extensions;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class ObservableExtensionsTests
{
  private readonly ITestOutputHelper p_output;

  public ObservableExtensionsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 30000)]
  public async Task ScanAsyncTest()
  {
    var result = await Observable
      .Return(Unit.Default)
      .Repeat(10)
      .ScanAsync(0, async (_seed, _entry) =>
      {
        await Task.Delay(TimeSpan.FromMilliseconds(50));
        return Interlocked.Increment(ref _seed);
      });

    Assert.Equal(10, result);
  }


}
