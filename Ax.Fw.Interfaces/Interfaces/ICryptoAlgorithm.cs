using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface ICryptoAlgorithm
{
  Span<byte> Decrypt(ReadOnlySpan<byte> _data);
  Span<byte> Encrypt(ReadOnlySpan<byte> _data);
}