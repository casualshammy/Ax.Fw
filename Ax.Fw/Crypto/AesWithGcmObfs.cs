using Ax.Fw.SharedTypes.Data.Crypto;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ax.Fw.Crypto;

public class AesWithGcmObfs : ICryptoAlgorithm
{
  private readonly AesGcm p_aesGcm;
  private readonly int p_minChunkSize;
  private long p_nonce;
  private int p_xorSeed;

  public AesWithGcmObfs(
    IReadOnlyLifetime _lifetime,
    string _key,
    int _minChunkSize,
    EncryptionKeyLength _keyLength = EncryptionKeyLength.Bits256)
  {
    if (_keyLength != EncryptionKeyLength.Bits128 && _keyLength != EncryptionKeyLength.Bits192 && _keyLength != EncryptionKeyLength.Bits256)
      throw new ArgumentOutOfRangeException(nameof(_keyLength), $"Key length must be 128, 192, or 256 bits");

    p_minChunkSize = _minChunkSize;
    p_nonce = GetRandomNegativeInt64();

    var key = Encoding.UTF8.GetBytes(_key);
    var hashSource = SHA512.HashData(key);

    p_xorSeed = BinaryPrimitives.ReadInt32LittleEndian(hashSource.AsSpan().Slice(16, 4));
    p_aesGcm = _lifetime.ToDisposeOnEnding(new AesGcm(hashSource[..((int)_keyLength / 8)]));
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data)
  {
    const int payloadHeaderSize = 4;
    const int nonceHeaderSize = 4;
    const int tagHeaderSize = 4;
    var nonceSize = AesGcm.NonceByteSizes.MaxSize;
    var tagSize = AesGcm.TagByteSizes.MaxSize;
    var payloadSize = nonceHeaderSize + nonceSize + tagHeaderSize + tagSize + _data.Length;
    var encryptedDataLength = Math.Max(payloadSize + payloadHeaderSize, p_minChunkSize);

    Span<byte> result = new byte[encryptedDataLength];
    var nonce = result.Slice(payloadHeaderSize + nonceHeaderSize, nonceSize);
    var tag = result.Slice(payloadHeaderSize + nonceHeaderSize + nonceSize + tagHeaderSize, tagSize);
    var cipherBytes = result.Slice(payloadHeaderSize + nonceHeaderSize + nonceSize + tagHeaderSize + tagSize, _data.Length);

    BinaryPrimitives.WriteInt32LittleEndian(result[..payloadHeaderSize], payloadSize ^ p_xorSeed); // 0 - 4
    BinaryPrimitives.WriteInt32LittleEndian(result.Slice(payloadHeaderSize, nonceHeaderSize), nonceSize ^ p_xorSeed); // 4 - 8
    BinaryPrimitives.WriteInt64LittleEndian(result.Slice(payloadHeaderSize + nonceHeaderSize, nonceSize), Interlocked.Increment(ref p_nonce)); // 8 - nonceSize+8
    BinaryPrimitives.WriteInt32LittleEndian(result.Slice(payloadHeaderSize + nonceHeaderSize + nonceSize, 4), tagSize ^ p_xorSeed); // nonceSize+8 - nonceSize+12

    p_aesGcm.Encrypt(nonce, _data, cipherBytes, tag);

    var requiredRandomBytesLength = p_minChunkSize - payloadSize - payloadHeaderSize;
    if (requiredRandomBytesLength > 0)
      Random.Shared.NextBytes(result.Slice(payloadSize + payloadHeaderSize, requiredRandomBytesLength));

    return result;
  }

  public Span<byte> Decrypt(ReadOnlySpan<byte> _data)
  {
    const int dataLengthHeaderSize = 4;
    var payloadSize = BinaryPrimitives.ReadInt32LittleEndian(_data[..dataLengthHeaderSize]) ^ p_xorSeed;
    var nonceSize = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(dataLengthHeaderSize, 4)) ^ p_xorSeed;
    var tagSize = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(dataLengthHeaderSize + 4 + nonceSize, 4)) ^ p_xorSeed;
    var cipherSize = payloadSize - 4 - nonceSize - 4 - tagSize;

    var nonce = _data.Slice(dataLengthHeaderSize + 4, nonceSize);
    var tag = _data.Slice(dataLengthHeaderSize + 4 + nonceSize + 4, tagSize);
    var cipherBytes = _data.Slice(dataLengthHeaderSize + 4 + nonceSize + 4 + tagSize, cipherSize);

    Span<byte> result = new byte[cipherSize];
    p_aesGcm.Decrypt(nonce, cipherBytes, tag, result);
    return result;
  }

  private static long GetRandomNegativeInt64()
  {
    Span<byte> nonceBuffer = new byte[8];
    Utilities.SharedRandom.NextBytes(nonceBuffer);
    nonceBuffer[7] = (byte)Utilities.SharedRandom.Next(128, 256);
    var result = BinaryPrimitives.ReadInt64LittleEndian(nonceBuffer);
    return result;
  }

}
