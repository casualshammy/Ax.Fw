using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ax.Fw.Crypto;

#if NET6_0_OR_GREATER
public class AesCbc : ICryptoAlgorithm
{
  private readonly byte[] p_iv;
  private readonly Aes p_aes;

  public AesCbc(IReadOnlyLifetime _lifetime, string _key, int _keyLengthBits = 256)
  {
    if (_keyLengthBits != 128 && _keyLengthBits != 256)
      throw new ArgumentOutOfRangeException(nameof(_keyLengthBits), $"Key length must be 16 or 32 bytes (128 or 256 bits)");

    var key = Encoding.UTF8.GetBytes(_key);
    using var sha = SHA512.Create();
    var hashSource = sha.ComputeHash(key);

    p_aes = _lifetime.ToDisposeOnEnding(Aes.Create());
    p_aes.KeySize = _keyLengthBits;
    p_aes.BlockSize = 128;
    p_iv = hashSource
      .Reverse()
      .Take(p_aes.BlockSize / 8)
      .ToArray();
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data) => p_aes.EncryptCbc(_data, p_iv, PaddingMode.PKCS7);

  public Span<byte> Decrypt(ReadOnlySpan<byte> _data) => p_aes.DecryptCbc(_data, p_iv, PaddingMode.PKCS7);

}
#endif