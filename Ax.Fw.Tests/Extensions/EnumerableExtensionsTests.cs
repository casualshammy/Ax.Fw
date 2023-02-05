using Ax.Fw.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Extensions;

public class EnumerableExtensionsTests
{
  private readonly ITestOutputHelper p_output;

  public EnumerableExtensionsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 10000)]
  public async Task OrderByAsyncTest()
  {
    var data = new[] { 10, 5, 9, 8, 6, 3, 1, 2, 4, 7, 0 };
    var ideal = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    static Task<int> comparer(int _a, int _b, CancellationToken _ct)
    {
      return Task.FromResult(_a.CompareTo(_b));
    }

    var sortedData = await data.OrderByAsync(comparer);

    Assert.Equal(ideal, sortedData);
  }

  [Fact(Timeout = 10000)]
  public async Task OrderByAsyncOneValueTest()
  {
    var data = new[] { 10 };
    var ideal = new[] { 10 };

    static Task<int> comparer(int _a, int _b, CancellationToken _ct)
    {
      Assert.Fail("Unneccessary sorting!");
      return Task.FromResult(_a.CompareTo(_b));
    }

    var sortedData = await data.OrderByAsync(comparer);

    Assert.Equal(ideal, sortedData);
  }

  [Fact(Timeout = 10000)]
  public async Task OrderByAsyncTwoValuesTest()
  {
    var data = new[] { 10, 5 };
    var ideal = new[] { 5, 10 };

    static Task<int> comparer(int _a, int _b, CancellationToken _ct)
    {
      return Task.FromResult(_a.CompareTo(_b));
    }

    var sortedData = await data.OrderByAsync(comparer);

    Assert.Equal(ideal, sortedData);
  }

  [Fact(Timeout = 10000)]
  public async Task OrderByAsyncTwoValuesEqualTest()
  {
    var data = new[] { 10, 5, 9, 8, 6, 3, 1, 2, 4, 7, 0, 4, 5, 6 };
    var ideal = new[] { 0, 1, 2, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 10 };

    static Task<int> comparer(int _a, int _b, CancellationToken _ct)
    {
      return Task.FromResult(_a.CompareTo(_b));
    }

    var sortedData = await data.OrderByAsync(comparer);

    Assert.Equal(ideal, sortedData);
  }


}
