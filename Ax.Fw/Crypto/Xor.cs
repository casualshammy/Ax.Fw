using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Ax.Fw.Crypto;

public class Xor : ICryptoAlgorithm
{
  private const int CHUNK_SIZE = 128 * 1024;
  private readonly byte[] p_mergeArray;

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
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data) => Transform2(_data);

  public Span<byte> Decrypt(ReadOnlySpan<byte> _data) => Transform2(_data);

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

  private unsafe Span<byte> Transform2(ReadOnlySpan<byte> _data)
  {
    Span<byte> result = new byte[_data.Length];
    var chunks = (int)Math.Floor(_data.Length / (double)8);
    fixed (byte* dataPtr = _data)
    fixed (byte* resultPtr = result)
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
      result[index] = (byte)(_data[index] ^ p_mergeArray[lastCounter++]);

    return result;
  }
  
}