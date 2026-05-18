using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Ax.Fw.SharedTypes.Data;

/// <summary>
/// Creates a GUID with the provided data encoded in the leading bytes
/// and the remaining bytes filled with random data.
/// </summary>
public static class OrderedGuid
{
  /// <summary>
  /// Creates a GUID with a 4-byte prefix encoded from <paramref name="_data"/>.
  /// </summary>
  /// <param name="_data">The value written to the beginning of the GUID.</param>
  /// <returns>A new GUID with the specified prefix.</returns>
  public static Guid NewGuid(int _data)
  {
    Span<byte> bytes = stackalloc byte[16];
    BinaryPrimitives.WriteInt32LittleEndian(bytes, _data);
    RandomNumberGenerator.Fill(bytes[4..]);
    return new Guid(bytes);
  }

  /// <summary>
  /// Creates a GUID with an 8-byte prefix encoded from <paramref name="_data"/>.
  /// </summary>
  /// <param name="_data">The value written to the beginning of the GUID.</param>
  /// <returns>A new GUID with the specified prefix.</returns>
  public static Guid NewGuid(long _data)
  {
    Span<byte> bytes = stackalloc byte[16];
    BinaryPrimitives.WriteInt64LittleEndian(bytes, _data);
    RandomNumberGenerator.Fill(bytes[8..]);
    return new Guid(bytes);
  }

  /// <summary>
  /// Creates a GUID with a prefix containing the UTC ticks of <paramref name="_dateTime"/>.
  /// </summary>
  /// <param name="_dateTime">The date and time whose UTC ticks are used as the prefix.</param>
  /// <returns>A new GUID with a time-based prefix.</returns>
  public static Guid NewGuid(DateTimeOffset _dateTime)
    => NewGuid(_dateTime.UtcTicks);
}
