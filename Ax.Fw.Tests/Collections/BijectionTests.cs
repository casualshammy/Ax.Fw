using Ax.Fw.Collections;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Collections;

public class BijectionTests
{
  private readonly ITestOutputHelper p_output;

  public BijectionTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact]
  public void Set_SinglePair_StoresPairCorrectly()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");

    Assert.True(bijection.TryGetByKey(1, out var value));
    Assert.Equal("one", value);
    Assert.True(bijection.TryGetByValue("one", out var key));
    Assert.Equal(1, key);
  }

  [Fact]
  public void Set_MultiplePairs_StoresAllPairsCorrectly()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    bijection.Set(2, "two");
    bijection.Set(3, "three");

    Assert.True(bijection.TryGetByKey(1, out var value1));
    Assert.Equal("one", value1);
    Assert.True(bijection.TryGetByKey(2, out var value2));
    Assert.Equal("two", value2);
    Assert.True(bijection.TryGetByKey(3, out var value3));
    Assert.Equal("three", value3);
  }

  [Fact]
  public void Set_OverwriteExistingKey_UpdatesMapping()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    bijection.Set(1, "uno");

    Assert.True(bijection.TryGetByKey(1, out var value));
    Assert.Equal("uno", value);
    Assert.False(bijection.TryGetByValue("one", out _));
    Assert.True(bijection.TryGetByValue("uno", out var key));
    Assert.Equal(1, key);
  }

  [Fact]
  public void Set_OverwriteExistingValue_UpdatesMapping()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    bijection.Set(2, "one");

    Assert.True(bijection.TryGetByValue("one", out var key));
    Assert.Equal(2, key);
    Assert.False(bijection.TryGetByKey(1, out _));
    Assert.True(bijection.TryGetByKey(2, out var value));
    Assert.Equal("one", value);
  }

  [Fact]
  public void Set_SwapKeyValuePairs_MaintainsBijection()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    bijection.Set(2, "two");
    bijection.Set(1, "two");

    Assert.True(bijection.TryGetByKey(1, out var value));
    Assert.Equal("two", value);
    Assert.False(bijection.TryGetByKey(2, out _));
    Assert.False(bijection.TryGetByValue("one", out _));
  }

  [Fact]
  public void TryGetByKey_NonExistentKey_ReturnsFalse()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");

    Assert.False(bijection.TryGetByKey(2, out var value));
    Assert.Null(value);
  }

  [Fact]
  public void TryGetByValue_NonExistentValue_ReturnsFalse()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");

    Assert.False(bijection.TryGetByValue("two", out var key));
    Assert.Equal(0, key);
  }

  [Fact]
  public void TryGetByKey_EmptyBijection_ReturnsFalse()
  {
    var bijection = new Bijection<int, string>();

    Assert.False(bijection.TryGetByKey(1, out var value));
    Assert.Null(value);
  }

  [Fact]
  public void TryGetByValue_EmptyBijection_ReturnsFalse()
  {
    var bijection = new Bijection<int, string>();

    Assert.False(bijection.TryGetByValue("one", out var key));
    Assert.Equal(0, key);
  }

  [Fact]
  public void RemoveByKey_ExistingKey_RemovesBothDirections()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    var removed = bijection.RemoveByKey(1);

    Assert.True(removed);
    Assert.False(bijection.TryGetByKey(1, out _));
    Assert.False(bijection.TryGetByValue("one", out _));
  }

  [Fact]
  public void RemoveByKey_NonExistentKey_ReturnsFalse()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    var removed = bijection.RemoveByKey(2);

    Assert.False(removed);
    Assert.True(bijection.TryGetByKey(1, out _));
  }

  [Fact]
  public void RemoveByValue_ExistingValue_RemovesBothDirections()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    var removed = bijection.RemoveByValue("one");

    Assert.True(removed);
    Assert.False(bijection.TryGetByKey(1, out _));
    Assert.False(bijection.TryGetByValue("one", out _));
  }

  [Fact]
  public void RemoveByValue_NonExistentValue_ReturnsFalse()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    var removed = bijection.RemoveByValue("two");

    Assert.False(removed);
    Assert.True(bijection.TryGetByValue("one", out _));
  }

  [Fact]
  public void GetEnumerator_ReturnAllPairs()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    bijection.Set(2, "two");
    bijection.Set(3, "three");

    var pairs = bijection.ToList();

    Assert.Equal(3, pairs.Count);
    Assert.Contains(pairs, p => p.Key == 1 && p.Value == "one");
    Assert.Contains(pairs, p => p.Key == 2 && p.Value == "two");
    Assert.Contains(pairs, p => p.Key == 3 && p.Value == "three");
  }

  [Fact]
  public void GetEnumerator_EmptyBijection_ReturnsEmpty()
  {
    var bijection = new Bijection<int, string>();

    var pairs = bijection.ToList();

    Assert.Empty(pairs);
  }

  [Fact]
  public void ComplexScenario_MultipleOperations_MaintainsConsistency()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "one");
    bijection.Set(2, "two");
    bijection.Set(3, "three");

    Assert.Equal(3, bijection.Count);

    bijection.Set(1, "uno");
    Assert.Equal(3, bijection.Count);
    Assert.False(bijection.TryGetByValue("one", out _));
    Assert.True(bijection.TryGetByValue("uno", out _));

    bijection.RemoveByKey(2);
    Assert.Equal(2, bijection.Count);
    Assert.False(bijection.TryGetByValue("two", out _));

    bijection.Set(4, "three");
    Assert.Equal(2, bijection.Count);
    Assert.False(bijection.TryGetByKey(3, out _));
    Assert.True(bijection.TryGetByKey(4, out var value));
    Assert.Equal("three", value);

    bijection.RemoveByValue("uno");
    Assert.Equal(1, bijection.Count);
    Assert.False(bijection.TryGetByKey(1, out _));

    var finalPairs = bijection.ToList();
    Assert.Single(finalPairs);
    Assert.Equal(4, finalPairs[0].Key);
    Assert.Equal("three", finalPairs[0].Value);
  }

  [Fact]
  public void Bijection_WithStringKeys_WorksCorrectly()
  {
    var bijection = new Bijection<string, int>();

    bijection.Set("one", 1);
    bijection.Set("two", 2);

    Assert.True(bijection.TryGetByKey("one", out var value1));
    Assert.Equal(1, value1);
    Assert.True(bijection.TryGetByValue(2, out var key2));
    Assert.Equal("two", key2);
  }

  [Fact]
  public void Set_CircularReplacement_MaintainsBijection()
  {
    var bijection = new Bijection<int, string>();

    bijection.Set(1, "a");
    bijection.Set(2, "b");
    bijection.Set(3, "c");

    bijection.Set(1, "b");

    Assert.True(bijection.TryGetByKey(1, out var value1));
    Assert.Equal("b", value1);
    Assert.False(bijection.TryGetByKey(2, out _));
    Assert.True(bijection.TryGetByKey(3, out var value3));
    Assert.Equal("c", value3);
    Assert.False(bijection.TryGetByValue("a", out _));
  }
}
