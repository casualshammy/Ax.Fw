using Ax.Fw.SharedTypes.Data;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw;

public static class Cryptography
{
  [Obsolete("Use 'Ax.Fw.Crypto.AesCbc' class static methods")]
  public static async Task EncryptAes(Stream _inStream, Stream _outEncryptedStream, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    await Crypto.AesCbc.EncryptAsync(_inStream, _outEncryptedStream, _password, _useFastHashing, _ct);
  }

  [Obsolete("Use 'Ax.Fw.Crypto.AesCbc' class static methods")]
  public static async Task<byte[]> EncryptAes(byte[] _data, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    return await Crypto.AesCbc.EncryptAsync(_data, _password, _useFastHashing, _ct);
  }

  [Obsolete("Use 'Ax.Fw.Crypto.AesCbc' class static methods")]
  public static async Task DecryptAes(Stream _inEncryptedStream, Stream _outStream, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    await Crypto.AesCbc.DecryptAsync(_inEncryptedStream, _outStream, _password, _useFastHashing, _ct);
  }

  [Obsolete("Use 'Ax.Fw.Crypto.AesCbc' class static methods")]
  public static async Task<byte[]> DecryptAes(byte[] _encryptedData, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    return await Crypto.AesCbc.DecryptAsync(_encryptedData, _password, _useFastHashing, _ct);
  }

  public static string CalculateSHAHash(string _data, HashComplexity _hashComplexity = HashComplexity.Bit512)
  {
    var data = Encoding.UTF8.GetBytes(_data);
    var hash = _hashComplexity switch
    {
      HashComplexity.Bit256 => SHA256.HashData(data),
      HashComplexity.Bit384 => SHA384.HashData(data),
      HashComplexity.Bit512 => SHA512.HashData(data),
      _ => throw new InvalidOperationException($"Don't know how to calculate {_hashComplexity} hash!")
    };

    return BitConverter.ToString(hash).Replace("-", "");
  }

  public static string CalculateMd5Hash(string _data)
  {
    var data = Encoding.UTF8.GetBytes(_data);
    var hash = MD5.HashData(data);
    return BitConverter.ToString(hash).Replace("-", "");
  }

}
