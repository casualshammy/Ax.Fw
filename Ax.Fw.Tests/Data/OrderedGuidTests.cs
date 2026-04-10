using Ax.Fw.SharedTypes.Data;
using System;
using System.Buffers.Binary;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Data;

public class OrderedGuidTests
{
  private readonly ITestOutputHelper p_output;

  public OrderedGuidTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(-1)]
  [InlineData(int.MaxValue)]
  [InlineData(int.MinValue)]
  public void NewGuid_WithInt_PrefixIsCorrect(int _value)
  {
    var guid = OrderedGuid.NewGuid(_value);
    var bytes = guid.ToByteArray();
    var prefix = BinaryPrimitives.ReadInt32LittleEndian(bytes);
    Assert.Equal(_value, prefix);
  }

  [Theory]
  [InlineData(0L)]
  [InlineData(1L)]
  [InlineData(-1L)]
  [InlineData(long.MaxValue)]
  [InlineData(long.MinValue)]
  public void NewGuid_WithLong_PrefixIsCorrect(long _value)
  {
    var guid = OrderedGuid.NewGuid(_value);
    var bytes = guid.ToByteArray();
    var prefix = BinaryPrimitives.ReadInt64LittleEndian(bytes);
    Assert.Equal(_value, prefix);
  }

  [Fact]
  public void NewGuid_WithDateTimeOffset_PrefixMatchesUtcTicks()
  {
    var dateTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(3));
    var guid = OrderedGuid.NewGuid(dateTime);
    var bytes = guid.ToByteArray();
    var prefix = BinaryPrimitives.ReadInt64LittleEndian(bytes);
    Assert.Equal(dateTime.UtcTicks, prefix);
  }

  [Fact]
  public void NewGuid_WithDateTimeOffset_UsesUtcNotLocalOffset()
  {
    // Same instant, different UTC offsets ? prefixes must be equal
    var utcTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
    var offsetTime = new DateTimeOffset(2024, 6, 15, 15, 0, 0, TimeSpan.FromHours(3));

    var prefixUtc = BinaryPrimitives.ReadInt64LittleEndian(OrderedGuid.NewGuid(utcTime).ToByteArray());
    var prefixOffset = BinaryPrimitives.ReadInt64LittleEndian(OrderedGuid.NewGuid(offsetTime).ToByteArray());

    Assert.Equal(prefixUtc, prefixOffset);
  }

  [Fact]
  public void NewGuid_WithInt_IsUniqueEachCall()
  {
    var guid1 = OrderedGuid.NewGuid(42);
    var guid2 = OrderedGuid.NewGuid(42);
    Assert.NotEqual(guid1, guid2);
  }

  [Fact]
  public void NewGuid_WithLong_IsUniqueEachCall()
  {
    var guid1 = OrderedGuid.NewGuid(42L);
    var guid2 = OrderedGuid.NewGuid(42L);
    Assert.NotEqual(guid1, guid2);
  }

  [Fact]
  public void NewGuid_WithDateTimeOffset_IsUniqueEachCall()
  {
    var dateTime = DateTimeOffset.UtcNow;
    var guid1 = OrderedGuid.NewGuid(dateTime);
    var guid2 = OrderedGuid.NewGuid(dateTime);
    Assert.NotEqual(guid1, guid2);
  }
}
