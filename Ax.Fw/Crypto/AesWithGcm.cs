using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ax.Fw.Crypto;

public class AesWithGcm
{
  private readonly AesGcm p_aesGcm;
  private long p_nonce = long.MinValue;

  public AesWithGcm(string _key, int _keyLengthBits = 256)
  {
    if (_keyLengthBits != 128 && _keyLengthBits != 192 && _keyLengthBits != 256)
      throw new ArgumentOutOfRangeException(nameof(_keyLengthBits), $"Key length must be 16, 24, or 32 bytes (128, 192, or 256 bits)");

    var key = Encoding.UTF8.GetBytes(_key);
    using var sha = SHA512.Create();
    var hashSource = sha.ComputeHash(key);
    p_aesGcm = new(hashSource.Take(_keyLengthBits/8).ToArray());
  }

  public byte[] Encrypt(byte[] _data)
  {
    var nonceSize = AesGcm.NonceByteSizes.MaxSize;
    var tagSize = AesGcm.TagByteSizes.MaxSize;
    var encryptedDataLength = 4 + nonceSize + 4 + tagSize + _data.Length;

    var result = encryptedDataLength < 1024 ? stackalloc byte[encryptedDataLength] : new byte[encryptedDataLength];
    var nonce = result.Slice(4, nonceSize);
    var tag = result.Slice(4 + nonceSize + 4, tagSize);
    var cipherBytes = result.Slice(4 + nonceSize + 4 + tagSize, _data.Length);

    BinaryPrimitives.WriteInt32LittleEndian(result[..4], nonceSize);
    BinaryPrimitives.WriteInt64LittleEndian(result.Slice(4, nonceSize), Interlocked.Increment(ref p_nonce));
    BinaryPrimitives.WriteInt32LittleEndian(result.Slice(4 + nonceSize, 4), tagSize);

    p_aesGcm.Encrypt(nonce, _data, cipherBytes, tag);
    return result.ToArray();
  }

  public byte[] Decrypt(byte[] _data)
  {
    var encryptedData = _data.AsSpan();

    var nonceSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData[..4]);
    var tagSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4));
    var cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

    var nonce = encryptedData.Slice(4, nonceSize);
    var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
    var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

    var result = cipherSize < 1024 ? stackalloc byte[cipherSize] : new byte[cipherSize];

    p_aesGcm.Decrypt(nonce, cipherBytes, tag, result);
    return result.ToArray();
  }
}
