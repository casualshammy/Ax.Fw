using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Ax.Fw.Crypto;

public class Xor : ICryptoAlgorithm
{
  private readonly byte[] p_keyArray;
  private readonly long p_magicWord;

  public Xor(byte[] _key, int _keySize = 1024)
  {
    if (_keySize % (512 / 8) != 0)
      throw new ArgumentOutOfRangeException(nameof(_keySize), $"Key size must be divisible by 64!");

    Span<byte> buffer = new byte[_keySize];
    var hash = SHA512.HashData(_key);
    for (var i = 0; i < buffer.Length / hash.Length; i++)
    {
      var mergeSlice = buffer.Slice(i * hash.Length, hash.Length);
      hash = SHA512.HashData(hash);
      hash.CopyTo(mergeSlice);
    }
    p_keyArray = buffer.ToArray();
    p_magicWord = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(237, 8));
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

  private unsafe void Transform2(ReadOnlySpan<byte> _data, Span<byte> _result)
  {
    var chunks = (int)Math.Floor(_data.Length / (double)8);
    fixed (byte* dataPtr = _data)
    fixed (byte* resultPtr = _result)
    fixed (byte* keyPtr = p_keyArray)
    {
      long* dataLongPtr = (long*)dataPtr;
      long* resultLongPtr = (long*)resultPtr;
      long* keyLongPtr = (long*)keyPtr;
      long* keyLongStartPtr = (long*)keyPtr;

      var counter = 0;
      var maxLength = p_keyArray.Length / 8;
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
      _result[index] = (byte)(_data[index] ^ p_keyArray[lastCounter++]);
  }

}