using Ax.Fw.Extensions;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Extensions;

public class ArrayExtensionsTests
{
  private readonly ITestOutputHelper p_output;

  public ArrayExtensionsTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact]
  public void TestArrayDeconstuction()
  {
    var array0 = Array.Empty<int>();
    var array1 = new int[] { 1 };
    var array2 = new int[] { 1, 2 };
    var array3 = new int[] { 1, 2, 3 };
    var array4 = new int[] { 1, 2, 3, 4 };
    var array5 = new int[] { 1, 2, 3, 4, 5 };

    Assert.Throws<InvalidOperationException>(() =>
    {
      var (a1, a2) = array0;
    });
    Assert.Throws<InvalidOperationException>(() =>
    {
      var (a1, a2) = array1;
    });
    Assert.Throws<InvalidOperationException>(() =>
    {
      var (a1, a2, a3, a4, a5) = array4;
    });

    {
      var (a1, a2) = array2;
      Assert.Equal(1, a1);
      Assert.Equal(2, a2);
    }

    {
      var (a1, a2, a3) = array3;
      Assert.Equal(1, a1);
      Assert.Equal(2, a2);
      Assert.Equal(3, a3);
    }

    {
      var (a1, a2, a3, a4) = array4;
      Assert.Equal(1, a1);
      Assert.Equal(2, a2);
      Assert.Equal(3, a3);
      Assert.Equal(4, a4);
    }

    {
      var (a1, a2, a3, a4, a5) = array5;
      Assert.Equal(1, a1);
      Assert.Equal(2, a2);
      Assert.Equal(3, a3);
      Assert.Equal(4, a4);
      Assert.Equal(5, a5);
    }

    {
      var (a1, a2) = array5;
      Assert.Equal(1, a1);
      Assert.Equal(2, a2);
    }

  }

}
