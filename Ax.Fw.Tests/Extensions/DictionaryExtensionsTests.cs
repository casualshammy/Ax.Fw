using Ax.Fw.Extensions;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Extensions
{
  public class DictionaryExtensionsTests
  {
    private readonly ITestOutputHelper p_output;

    public DictionaryExtensionsTests(ITestOutputHelper _output)
    {
      p_output = _output;
    }

    [Fact]
    public void GetDictionaryHashCodeTest()
    {
      var dictionary = new Dictionary<string, string?>
      {
        ["1"] = "0-1",
        ["2"] = null,
        ["3"] = "",
        ["4"] = "0-4"
      };

      var anotherDictionary = new Dictionary<string, string?>();
      Assert.NotEqual(dictionary.GetDictionaryHashCode(), anotherDictionary.GetDictionaryHashCode());

      anotherDictionary["1"] = "0-1";
      Assert.NotEqual(dictionary.GetDictionaryHashCode(), anotherDictionary.GetDictionaryHashCode());

      anotherDictionary["2"] = null;
      Assert.NotEqual(dictionary.GetDictionaryHashCode(), anotherDictionary.GetDictionaryHashCode());

      anotherDictionary["3"] = "";
      Assert.NotEqual(dictionary.GetDictionaryHashCode(), anotherDictionary.GetDictionaryHashCode());

      anotherDictionary["4"] = "0-3";
      Assert.NotEqual(dictionary.GetDictionaryHashCode(), anotherDictionary.GetDictionaryHashCode());

      anotherDictionary["4"] = "0-4";
      Assert.Equal(dictionary.GetDictionaryHashCode(), anotherDictionary.GetDictionaryHashCode());

      anotherDictionary["5"] = "0-5";
      Assert.NotEqual(dictionary.GetDictionaryHashCode(), anotherDictionary.GetDictionaryHashCode());
    }

    [Fact]
    public void DictionaryEqualsTest()
    {
      var dictionary = new Dictionary<string, string?>
      {
        ["1"] = "0-1",
        ["2"] = null,
        ["3"] = "",
        ["4"] = "0-4"
      };

      var anotherDictionary = new Dictionary<string, string?>();
      Assert.False(dictionary.DictionaryEquals(anotherDictionary));

      anotherDictionary["1"] = "0-1";
      Assert.False(dictionary.DictionaryEquals(anotherDictionary));

      anotherDictionary["2"] = null;
      Assert.False(dictionary.DictionaryEquals(anotherDictionary));

      anotherDictionary["3"] = "";
      Assert.False(dictionary.DictionaryEquals(anotherDictionary));

      anotherDictionary["4"] = "0-3";
      Assert.False(dictionary.DictionaryEquals(anotherDictionary));

      anotherDictionary["4"] = "0-4";
      Assert.True(dictionary.DictionaryEquals(anotherDictionary));

      anotherDictionary["5"] = "0-5";
      Assert.False(dictionary.DictionaryEquals(anotherDictionary));
    }

    [Fact]
    public void ComputeAverageChunks_BasicTest()
    {
      // Arrange
      var data = new List<KeyValuePair<long, long>>
            {
                new KeyValuePair<long, long>(1, 10),
                new KeyValuePair<long, long>(2, 20),
                new KeyValuePair<long, long>(3, 30),
                new KeyValuePair<long, long>(4, 40),
                new KeyValuePair<long, long>(5, 50)
            };

      // Act
      var result = data.ComputeAverageChunks(2, 1, 5);

      // Assert
      Assert.Equal(2, result.Count);
      Assert.Equal(20, result[3]); // Average for [1, 3]
      Assert.Equal(45, result[5]); // Average for [4, 5]
    }

    [Fact]
    public void ComputeAverageChunks_EmptyInput()
    {
      // Arrange
      var data = new List<KeyValuePair<long, long>>();

      // Act
      var result = data.ComputeAverageChunks(3, 1, 10);

      // Assert
      Assert.Equal(3, result.Count);
      foreach (var value in result.Values)
      {
        Assert.Equal(0, value);
      }
    }

    [Fact]
    public void ComputeAverageChunks_ValuesOutsideRange()
    {
      // Arrange
      var data = new List<KeyValuePair<long, long>>
            {
                new KeyValuePair<long, long>(-5, 10), // Outside range
                new KeyValuePair<long, long>(1, 20),
                new KeyValuePair<long, long>(10, 30),
                new KeyValuePair<long, long>(15, 40), // Outside range
            };

      // Act
      var result = data.ComputeAverageChunks(2, 1, 10);

      // Assert
      Assert.Equal(2, result.Count);
      Assert.Equal(20, result[5.5]); // Average for [1, 5]
      Assert.Equal(30, result[10]); // Average for [6, 10]
    }

    [Fact]
    public void ComputeAverageChunks_SingleChunk()
    {
      // Arrange
      var data = new List<KeyValuePair<long, long>>
            {
                new KeyValuePair<long, long>(1, 10),
                new KeyValuePair<long, long>(2, 20),
                new KeyValuePair<long, long>(3, 30)
            };

      // Act
      var result = data.ComputeAverageChunks(1, 1, 3);

      // Assert
      Assert.Single(result);
      Assert.Equal(20, result[3]); // Average for [1, 3]
    }

    [Fact]
    public void ComputeAverageChunks_InvalidRange_ThrowsException()
    {
      // Arrange
      var data = new List<KeyValuePair<long, long>>();

      // Act & Assert
      Assert.Throws<ArgumentException>(() => data.ComputeAverageChunks(2, 10, 1));
    }

    [Fact]
    public void ComputeAverageChunks_StepTooSmall_ThrowsException()
    {
      // Arrange
      var data = new List<KeyValuePair<long, long>>();

      // Act & Assert
      Assert.Throws<ArgumentException>(() => data.ComputeAverageChunks(100, 1, 10));
    }

    [Fact]
    public void ComputeAverageChunks_DuplicateKeys()
    {
      // Arrange
      var data = new List<KeyValuePair<long, long>>
            {
                new KeyValuePair<long, long>(1, 10),
                new KeyValuePair<long, long>(1, 20),
                new KeyValuePair<long, long>(2, 30),
                new KeyValuePair<long, long>(2, 40)
            };

      // Act
      var result = data.ComputeAverageChunks(2, 1, 3);

      // Assert
      Assert.Equal(2, result.Count);
      Assert.Equal(25, result[2]); // Average for [1, 2]
      Assert.Equal(0, result[3]); // No values in [3]
    }

  }
}
