using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ax.Fw.Crypto;

#if NET6_0_OR_GREATER
public class ChaCha20WithPoly1305 : ICryptoAlgorithm
{
  private readonly ChaCha20Poly1305 p_chacha;
  private long p_nonce = long.MinValue;

  public ChaCha20WithPoly1305(IReadOnlyLifetime _lifetime, string _key)
  {
    var key = Encoding.UTF8.GetBytes(_key);
    using var sha = SHA512.Create();
    var hashSource = sha.ComputeHash(key);
    p_chacha = _lifetime.ToDisposeOnEnding(new ChaCha20Poly1305(hashSource.Take(32).ToArray()));
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data)
  {
    const int nonceSize = 12;
    const int tagSize = 16;
    var encryptedDataLength = 4 + nonceSize + 4 + tagSize + _data.Length;

    Span<byte> result = new byte[encryptedDataLength];
    var nonce = result.Slice(4, nonceSize);
    var tag = result.Slice(4 + nonceSize + 4, tagSize);
    var cipherBytes = result.Slice(4 + nonceSize + 4 + tagSize, _data.Length);

    BinaryPrimitives.WriteInt32LittleEndian(result[..4], nonceSize);
    BinaryPrimitives.WriteInt64LittleEndian(result.Slice(4, nonceSize), Interlocked.Increment(ref p_nonce));
    BinaryPrimitives.WriteInt32LittleEndian(result.Slice(4 + nonceSize, 4), tagSize);

    p_chacha.Encrypt(nonce, _data, cipherBytes, tag);
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

    var result = new byte[cipherSize];

    p_chacha.Decrypt(nonce, cipherBytes, tag, result);
    return result;
  }

}
#endif