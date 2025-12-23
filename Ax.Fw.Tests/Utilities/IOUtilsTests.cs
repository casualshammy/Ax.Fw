using System;
using System.Reactive;
using Xunit;

namespace Ax.Fw.Tests.Utilities;

public class IOUtilsTests
{
  [Fact]
  public void TryParseArgument_StringValue_Success()
  {
    var args = new[] { "--name", "John" };
    var result = IOUtils.TryParseArgument(args, "--name", x => x, out string? value);

    Assert.True(result);
    Assert.Equal("John", value);
  }

  [Fact]
  public void TryParseArgument_IntValue_Success()
  {
    var args = new[] { "--port", "8080" };
    var result = IOUtils.TryParseArgument(args, "--port", int.Parse, out int value);

    Assert.True(result);
    Assert.Equal(8080, value);
  }

  [Fact]
  public void TryParseArgument_BoolValue_Success()
  {
    var args = new[] { "--enabled", "true" };
    var result = IOUtils.TryParseArgument(args, "--enabled", bool.Parse, out bool value);

    Assert.True(result);
    Assert.True(value);
  }

  [Fact]
  public void TryParseArgument_UnitValue_Success()
  {
    var args = new[] { "--verbose" };
    var result = IOUtils.TryParseArgument(args, "--verbose", _ => Unit.Default, out Unit value);

    Assert.True(result);
    Assert.Equal(Unit.Default, value);
  }

  [Fact]
  public void TryParseArgument_UnitValueWithOtherArgs_Success()
  {
    var args = new[] { "--verbose", "--name", "test" };
    var result = IOUtils.TryParseArgument(args, "--verbose", _ => Unit.Default, out Unit value);

    Assert.True(result);
    Assert.Equal(Unit.Default, value);
  }

  [Fact]
  public void TryParseArgument_ArgumentNotFound_ReturnsFalse()
  {
    var args = new[] { "--name", "John" };
    var result = IOUtils.TryParseArgument(args, "--age", int.Parse, out int value);

    Assert.False(result);
    Assert.Equal(0, value);
  }

  [Fact]
  public void TryParseArgument_EmptyArgs_ReturnsFalse()
  {
    var args = Array.Empty<string>();
    var result = IOUtils.TryParseArgument(args, "--name", x => x, out string? value);

    Assert.False(result);
    Assert.Null(value);
  }

  [Fact]
  public void TryParseArgument_ArgumentAtEnd_ReturnsFalse()
  {
    var args = new[] { "--name", "John", "--port" };
    var result = IOUtils.TryParseArgument(args, "--port", int.Parse, out int value);

    Assert.False(result);
    Assert.Equal(0, value);
  }

  [Fact]
  public void TryParseArgument_ValueStartsWithDashes_ReturnsFalse()
  {
    var args = new[] { "--name", "--value" };
    var result = IOUtils.TryParseArgument(args, "--name", x => x, out string? value);

    Assert.False(result);
    Assert.Null(value);
  }

  [Fact]
  public void TryParseArgument_ArgumentNameWithoutDashes_ThrowsException()
  {
    var args = new[] { "name", "John" };
    
    Assert.Throws<ArgumentException>(() => 
      IOUtils.TryParseArgument(args, "name", x => x, out string? value));
  }

  [Fact]
  public void TryParseArgument_MultipleArguments_ParsesCorrectly()
  {
    var args = new[] { "--name", "John", "--age", "25", "--city", "London" };
    
    var nameResult = IOUtils.TryParseArgument(args, "--name", x => x, out string? name);
    var ageResult = IOUtils.TryParseArgument(args, "--age", int.Parse, out int age);
    var cityResult = IOUtils.TryParseArgument(args, "--city", x => x, out string? city);

    Assert.True(nameResult);
    Assert.Equal("John", name);
    
    Assert.True(ageResult);
    Assert.Equal(25, age);
    
    Assert.True(cityResult);
    Assert.Equal("London", city);
  }

  [Fact]
  public void TryParseArgument_ValueWithSpaces_ParsesCorrectly()
  {
    var args = new[] { "--message", "\"Hello World\"" };
    var result = IOUtils.TryParseArgument(args, "--message", x => x, out string? value);

    Assert.True(result);
    Assert.Equal("Hello World", value);
  }

  [Fact]
  public void TryParseArgument_DuplicateArgument_ParsesFirst()
  {
    var args = new[] { "--name", "John", "--name", "Jane" };
    var result = IOUtils.TryParseArgument(args, "--name", x => x, out string? value);

    Assert.True(result);
    Assert.Equal("John", value);
  }

  [Fact]
  public void TryParseArgument_ArgumentAtBeginning_Success()
  {
    var args = new[] { "--first", "value", "other", "args" };
    var result = IOUtils.TryParseArgument(args, "--first", x => x, out string? value);

    Assert.True(result);
    Assert.Equal("value", value);
  }

  [Fact]
  public void TryParseArgument_ArgumentInMiddle_Success()
  {
    var args = new[] { "arg1", "--middle", "value", "arg2" };
    var result = IOUtils.TryParseArgument(args, "--middle", x => x, out string? value);

    Assert.True(result);
    Assert.Equal("value", value);
  }

  [Fact]
  public void TryParseArgument_NegativeNumber_Success()
  {
    var args = new[] { "--offset", "-100" };
    var result = IOUtils.TryParseArgument(args, "--offset", int.Parse, out int value);

    Assert.True(result);
    Assert.Equal(-100, value);
  }

  [Fact]
  public void TryParseArgument_DecimalNumber_Success()
  {
    var args = new[] { "--ratio", "3.14" };
    var result = IOUtils.TryParseArgument(args, "--ratio", double.Parse, out double value);

    Assert.True(result);
    Assert.Equal(3.14, value);
  }
}
