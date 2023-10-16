using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ax.Fw.Crypto;

public class AesWithGcm : ICryptoAlgorithm
{
  private readonly AesGcm p_aesGcm;
  private long p_nonce;

  public AesWithGcm(IReadOnlyLifetime _lifetime, string _key, int _keyLengthBits = 256)
  {
    if (_keyLengthBits != 128 && _keyLengthBits != 192 && _keyLengthBits != 256)
      throw new ArgumentOutOfRangeException(nameof(_keyLengthBits), $"Key length must be 16, 24, or 32 bytes (128, 192, or 256 bits)");

    p_nonce = GetRandomNegativeInt64();

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

  public static void EncryptStream(Stream _in, Stream _out, byte[] _key, int _keyLengthBits = 256, CancellationToken _ct = default)
  {
    if (!_in.CanRead)
      throw new InvalidOperationException($"Input stream is not readable!");
    if (!_out.CanWrite)
      throw new InvalidOperationException($"Output stream is not writable!");

    using var sha = SHA512.Create();
    var hashSource = sha.ComputeHash(_key);
    using var aesGcm = new AesGcm(hashSource.Take(_keyLengthBits / 8).ToArray());

    var nonceSize = AesGcm.NonceByteSizes.MaxSize;
    var tagSize = AesGcm.TagByteSizes.MaxSize;
    const int chunkSize = 64 * 1024;

    Span<byte> meta = new byte[4 + 4 + 4];
    BinaryPrimitives.WriteInt32LittleEndian(meta.Slice(0,4), nonceSize);
    BinaryPrimitives.WriteInt32LittleEndian(meta.Slice(4, 4), tagSize);
    BinaryPrimitives.WriteInt32LittleEndian(meta.Slice(8, 4), chunkSize);
    _out.Write(meta);

    var nonce = GetRandomNegativeInt64();
    Span<byte> inBuffer = new byte[chunkSize];
    Span<byte> outBuffer = new byte[nonceSize + tagSize + chunkSize];
    while (!_ct.IsCancellationRequested)
    {
      var inRead = FillBuffer(inBuffer, _in, _ct);
      if (inRead == 0)
        return;

      var inData = inBuffer.Slice(0, inRead);
      var outNonce = outBuffer.Slice(0, nonceSize);
      var outTag = outBuffer.Slice(nonceSize, tagSize);
      var outData = outBuffer.Slice(nonceSize + tagSize, Math.Min(inRead, chunkSize));

      BinaryPrimitives.WriteInt64LittleEndian(outNonce, Interlocked.Increment(ref nonce));
      aesGcm.Encrypt(outNonce, inData, outData, outTag);

      _out.Write(outBuffer.Slice(0, nonceSize + tagSize + inRead));
    }

    _ct.ThrowIfCancellationRequested();
  }

  public static void DecryptStream(Stream _in, Stream _out, byte[] _key, int _keyLengthBits = 256, CancellationToken _ct = default)
  {
    if (!_in.CanRead)
      throw new InvalidOperationException($"Input stream is not readable!");
    if (!_out.CanWrite)
      throw new InvalidOperationException($"Output stream is not writable!");

    using var sha = SHA512.Create();
    var hashSource = sha.ComputeHash(_key);
    using var aesGcm = new AesGcm(hashSource.Take(_keyLengthBits / 8).ToArray());

    Span<byte> meta = new byte[4 + 4 + 4];
    if (FillBuffer(meta, _in, _ct) != meta.Length)
      throw new InvalidOperationException("Can't read meta!");

    var nonceSize = BinaryPrimitives.ReadInt32LittleEndian(meta.Slice(0, 4));
    var tagSize = BinaryPrimitives.ReadInt32LittleEndian(meta.Slice(4, 4));
    var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(meta.Slice(8, 4));

    Span<byte> inBuffer = new byte[nonceSize + tagSize + chunkSize];
    Span<byte> outBuffer = new byte[chunkSize];

    while (!_ct.IsCancellationRequested)
    {
      var inRead = FillBuffer(inBuffer, _in, _ct);
      if (inRead == 0)
        return;

      var inNonce = inBuffer.Slice(0, nonceSize);
      var inTag = inBuffer.Slice(nonceSize, tagSize);
      var inData = inBuffer.Slice(nonceSize + tagSize, inRead - nonceSize - tagSize);
      var outData = outBuffer.Slice(0, inData.Length);

      aesGcm.Decrypt(inNonce, inData, inTag, outData);
      _out.Write(outData);
    }

    _ct.ThrowIfCancellationRequested();
  }

  private static int FillBuffer(Span<byte> _buffer, Stream _in, CancellationToken _ct)
  {
    var size = _buffer.Length;
    var bytesRead = _in.Read(_buffer);
    var lastRead = 0;
    while (!_ct.IsCancellationRequested && bytesRead < size && lastRead > 0)
    {
      lastRead = _in.Read(_buffer.Slice(bytesRead));
      bytesRead += lastRead;
    }

    _ct.ThrowIfCancellationRequested();
    return bytesRead;
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
