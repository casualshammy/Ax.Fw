using Ax.Fw.Mathematics;
using System;
using Xunit;

namespace Ax.Fw.Tests.Mathematics;

public class MedianFilterTests
{
  [Fact]
  public void Constructor_ShouldThrow_WhenWindowSizeIsEven()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new MedianFilter(4));
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenWindowSizeIsZeroOrNegative()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new MedianFilter(0));
    Assert.Throws<ArgumentOutOfRangeException>(() => new MedianFilter(-1));
  }

  [Fact]
  public void Calculate_WithWindowSize1_ShouldReturnOriginalArray()
  {
    var filter = new MedianFilter(1);
    var input = new[] { 5, 2, 8, 1, 9 };
    var result = filter.Calculate(input);

    Assert.Equal(input, result);
  }

  [Fact]
  public void Calculate_WithWindowSize3_ShouldFilterCorrectly()
  {
    var filter = new MedianFilter(3);
    var input = new[] { 1, 2, 3, 4, 5 };
    var result = filter.Calculate(input);

    var expected = new[] { 1, 2, 3, 4, 5 };
    Assert.Equal(expected, result);
  }

  [Fact]
  public void Calculate_ShouldRemoveOutliers()
  {
    var filter = new MedianFilter(3);
    var input = new[] { 1, 100, 2, 3, 200, 4 };
    var result = filter.Calculate(input);

    var expected = new[] { 1, 2, 3, 3, 4, 4 };
    Assert.Equal(expected, result);
  }

  [Fact]
  public void Calculate_WithWindowSize5_ShouldHandleBoundariesCorrectly()
  {
    var filter = new MedianFilter(5);
    var input = new[] { 1, 2, 3 };
    var result = filter.Calculate(input);

    var expected = new[] { 1, 2, 3 };
    Assert.Equal(expected, result);
  }

  [Fact]
  public void Calculate_WithSingleElement_ShouldReturnSameElement()
  {
    var filter = new MedianFilter(3);
    var input = new[] { 42 };
    var result = filter.Calculate(input);

    Assert.Single(result);
    Assert.Equal(42, result[0]);
  }

  [Fact]
  public void Calculate_WithEmptyArray_ShouldReturnEmptyArray()
  {
    var filter = new MedianFilter(3);
    var input = Array.Empty<int>();
    var result = filter.Calculate(input);

    Assert.Empty(result);
  }

  [Fact]
  public void Calculate_WithNegativeNumbers_ShouldWorkCorrectly()
  {
    var filter = new MedianFilter(3);
    var input = new[] { -5, -1, -10, -2, -8 };
    var result = filter.Calculate(input);

    var expected = new[] { -5, -5, -2, -8, -8 };
    Assert.Equal(expected, result);
  }
}