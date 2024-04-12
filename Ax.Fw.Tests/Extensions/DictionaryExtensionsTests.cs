using Ax.Fw.Extensions;
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

  }
}
