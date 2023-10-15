using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ax.Fw.Crypto;

public class AesWithGcm : ICryptoAlgorithm
{
  private readonly AesGcm p_aesGcm;
  private long p_nonce = long.MinValue;

  public AesWithGcm(IReadOnlyLifetime _lifetime, string _key, int _keyLengthBits = 256)
  {
    if (_keyLengthBits != 128 && _keyLengthBits != 192 && _keyLengthBits != 256)
      throw new ArgumentOutOfRangeException(nameof(_keyLengthBits), $"Key length must be 16, 24, or 32 bytes (128, 192, or 256 bits)");

    var key = Encoding.UTF8.GetBytes(_key);
    using var sha = SHA512.Create();
    var hashSource = sha.ComputeHash(key);
    p_aesGcm = _lifetime.ToDisposeOnEnding(new AesGcm(hashSource.Take(_keyLengthBits / 8).ToArray()));
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data)
  {
    var nonceSize = AesGcm.NonceByteSizes.MaxSize;
    var tagSize = AesGcm.TagByteSizes.MaxSize;
    var encryptedDataLength = 4 + nonceSize + 4 + tagSize + _data.Length;

    Span<byte> result = new byte[encryptedDataLength];
    var nonce = result.Slice(4, nonceSize);
    var tag = result.Slice(4 + nonceSize + 4, tagSize);
    var cipherBytes = result.Slice(4 + nonceSize + 4 + tagSize, _data.Length);

    BinaryPrimitives.WriteInt32LittleEndian(result[..4], nonceSize);
    BinaryPrimitives.WriteInt64LittleEndian(result.Slice(4, nonceSize), Interlocked.Increment(ref p_nonce));
    BinaryPrimitives.WriteInt32LittleEndian(result.Slice(4 + nonceSize, 4), tagSize);

    p_aesGcm.Encrypt(nonce, _data, cipherBytes, tag);
    return result;
  }

  public Span<byte> Decrypt(ReadOnlySpan<byte> _data)
  {
    var nonceSize = BinaryPrimitives.ReadInt32LittleEndian(_data[..4]);
    var tagSize = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(4 + nonceSize, 4));
    var cipherSize = _data.Length - 4 - nonceSize - 4 - tagSize;

    var nonce = _data.Slice(4, nonceSize);
    var tag = _data.Slice(4 + nonceSize + 4, tagSize);
    var cipherBytes = _data.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

    Span<byte> result = new byte[cipherSize];
    p_aesGcm.Decrypt(nonce, cipherBytes, tag, result);
    return result;
  }

}
