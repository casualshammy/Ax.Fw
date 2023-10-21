using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;

namespace Ax.Fw.Crypto;

public class Xor : ICryptoAlgorithm
{
  private const int CHUNK_SIZE = 128 * 1024;
  private readonly byte[] p_mergeArray;
  private readonly long p_magicWord;

  public Xor(byte[] _key)
  {
    Span<byte> mergeArray = new byte[CHUNK_SIZE];
    using var sha = SHA512.Create();
    var hashSource = sha.ComputeHash(_key);
    var hashSourceReverse = hashSource.Reverse().ToArray();
    var hashSourceLength = hashSource.Length;
    for (var i = 0; i < CHUNK_SIZE / hashSourceLength; i++)
    {
      var mergeSlice = mergeArray.Slice(i * hashSourceLength, hashSourceLength);
      if (i % 2 == 0)
        hashSource.CopyTo(mergeSlice);
      else
        hashSourceReverse.CopyTo(mergeSlice);
    }

    p_mergeArray = mergeArray.ToArray();
    p_magicWord = BinaryPrimitives.ReadInt64LittleEndian(p_mergeArray.AsSpan().Slice(1024, 8));
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data)
  {
    Span<byte> result = new byte[_data.Length + 8];
    BinaryPrimitives.WriteInt64LittleEndian(result.Slice(0, 8), p_magicWord);
    Transform2(_data, result.Slice(8));
    return result;
  }

  public Span<byte> Decrypt(ReadOnlySpan<byte> _data)
  {
    var magicWord = BinaryPrimitives.ReadInt64LittleEndian(_data.Slice(0, 8));
    if (magicWord != p_magicWord)
      throw new CryptographicException($"Can't decrypt message - header is invalid");

    Span<byte> result = new byte[_data.Length - 8];
    Transform2(_data.Slice(8), result);

    return result;
  }

  private Span<byte> Transform(ReadOnlySpan<byte> _data)
  {
    unchecked
    {
      var dataLength = _data.Length;
      Span<byte> result = new byte[dataLength];
      for (int i = 0; i < dataLength; i++)
        result[i] = (byte)(_data[i] ^ p_mergeArray[i % CHUNK_SIZE]);

      return result;
    }
  }

  private unsafe void Transform2(ReadOnlySpan<byte> _data, Span<byte> _result)
  {
    var chunks = (int)Math.Floor(_data.Length / (double)8);
    fixed (byte* dataPtr = _data)
    fixed (byte* resultPtr = _result)
    fixed (byte* keyPtr = p_mergeArray)
    {
      long* dataLongPtr = (long*)dataPtr;
      long* resultLongPtr = (long*)resultPtr;
      long* keyLongPtr = (long*)keyPtr;
      long* keyLongStartPtr = (long*)keyPtr;

      var counter = 0;
      var maxLength = CHUNK_SIZE / 8;
      for (int _ = 0; _ < chunks; _++)
      {
        *resultLongPtr = *dataLongPtr ^ *keyLongPtr;

        dataLongPtr++;
        resultLongPtr++;

        if (++counter >= maxLength)
          keyLongPtr = keyLongStartPtr;
        else
          keyLongPtr++;
      }
    }

    var lastCounter = 0;
    for (int index = chunks * 8; index < _data.Length; index++)
      _result[index] = (byte)(_data[index] ^ p_mergeArray[lastCounter++]);
  }

}