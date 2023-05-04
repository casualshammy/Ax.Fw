using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Extensions;

public class ObservableExtensionsTests
{
  private readonly ITestOutputHelper p_output;

  public ObservableExtensionsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 10000)]
  public async Task HotAliveTest()
  {
    using var lifetime = new Lifetime();

    var observableElementsCount = 5;

    var observable = Observable
      .Return(Unit.Default)
      .Repeat(observableElementsCount)
      .Publish()
      .RefCount();

    var counter = 0L;
    var lifeCompleteCounter = 0L;
    var life = (IReadOnlyLifetime?)null;

    observable
      .HotAlive(lifetime, (_entry, _life) =>
      {
        var oldLife = Interlocked.Exchange(ref life, _life);
        Assert.NotEqual(oldLife, _life);
        _life.DoOnCompleted(() => Interlocked.Increment(ref lifeCompleteCounter));
        Interlocked.Increment(ref counter);
      });

    await Task.Delay(1000);

    Assert.Equal(observableElementsCount, counter);
    Assert.Equal(observableElementsCount - 1, lifeCompleteCounter);
  }

}
