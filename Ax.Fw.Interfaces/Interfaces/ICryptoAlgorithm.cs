using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface ICryptoAlgorithm
{
  public ReadOnlySpan<byte> Decrypt(ReadOnlySpan<byte> _data);
  public ReadOnlySpan<byte> Encrypt(ReadOnlySpan<byte> _data);
}