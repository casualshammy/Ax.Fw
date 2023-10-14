using Ax.Fw.SharedTypes.Data;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ax.Fw;

public static class Cryptography
{
  public static async Task EncryptAes(Stream _inStream, Stream _outEncryptedStream, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    if (!_outEncryptedStream.CanWrite)
      throw new NotSupportedException($"Can't write to '{nameof(_outEncryptedStream)}'!");
    if (!_inStream.CanRead)
      throw new NotSupportedException($"Can't read from '{nameof(_inStream)}'!");

    using (var rijCrypto = Aes.Create())
    {
      rijCrypto.KeySize = 256;
      rijCrypto.BlockSize = 128;

      if (_useFastHashing)
      {
        using var hash = SHA512.Create();
        var key = hash.ComputeHash(_password);
        rijCrypto.Key = key.Take(rijCrypto.KeySize / 8).ToArray();
        rijCrypto.IV = key.Reverse().Take(rijCrypto.BlockSize / 8).ToArray();
      }
      else
      {
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
        rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
        rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
      }

      rijCrypto.Mode = CipherMode.CBC;

      using (var encryptor = rijCrypto.CreateEncryptor())
      using (var cryptoStream = new CryptoStream(_outEncryptedStream, encryptor, CryptoStreamMode.Write, true))
        await _inStream.CopyToAsync(cryptoStream, 80 * 1024, _ct);
    }
  }

  public static async Task<byte[]> EncryptAes(byte[] _data, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    using (var rijCrypto = Aes.Create())
    {
      rijCrypto.KeySize = 256;
      rijCrypto.BlockSize = 128;

      if (_useFastHashing)
      {
        using var hash = SHA512.Create();
        var key = hash.ComputeHash(_password);
        rijCrypto.Key = key.Take(rijCrypto.KeySize / 8).ToArray();
        rijCrypto.IV = key.Reverse().Take(rijCrypto.BlockSize / 8).ToArray();
      }
      else
      {
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
        rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
        rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
      }

      rijCrypto.Mode = CipherMode.CBC;

      using (var encryptedStream = new MemoryStream())
      {
        using (var encryptor = rijCrypto.CreateEncryptor())
        using (var dataStream = new MemoryStream(_data))
        using (var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write, true))
          await dataStream.CopyToAsync(cryptoStream, 80 * 1024, _ct);

        return encryptedStream.ToArray();
      }
    }
  }

  public static async Task DecryptAes(Stream _inEncryptedStream, Stream _outStream, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    if (!_outStream.CanWrite)
      throw new NotSupportedException($"Can't write to '{nameof(_outStream)}'!");
    if (!_inEncryptedStream.CanRead)
      throw new NotSupportedException($"Can't read from '{nameof(_inEncryptedStream)}'!");

    using (var rijCrypto = Aes.Create())
    {
      rijCrypto.KeySize = 256;
      rijCrypto.BlockSize = 128;

      if (_useFastHashing)
      {
        using var hash = SHA512.Create();
        var key = hash.ComputeHash(_password);
        rijCrypto.Key = key.Take(rijCrypto.KeySize / 8).ToArray();
        rijCrypto.IV = key.Reverse().Take(rijCrypto.BlockSize / 8).ToArray();
      }
      else
      {
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
        rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
        rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
      }

      rijCrypto.Mode = CipherMode.CBC;

      using (var decryptor = rijCrypto.CreateDecryptor())
      using (var cryptoStream = new CryptoStream(_inEncryptedStream, decryptor, CryptoStreamMode.Read, true))
        await cryptoStream.CopyToAsync(_outStream, 80 * 1024, _ct);
    }
  }

  public static async Task<byte[]> DecryptAes(byte[] _encryptedData, byte[] _password, bool _useFastHashing = false, CancellationToken _ct = default)
  {
    using (var rijCrypto = Aes.Create())
    {
      rijCrypto.KeySize = 256;
      rijCrypto.BlockSize = 128;

      if (_useFastHashing)
      {
        using var hash = SHA512.Create();
        var key = hash.ComputeHash(_password);
        rijCrypto.Key = key.Take(rijCrypto.KeySize / 8).ToArray();
        rijCrypto.IV = key.Reverse().Take(rijCrypto.BlockSize / 8).ToArray();
      }
      else
      {
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
        rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
        rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
      }

      rijCrypto.Mode = CipherMode.CBC;

      using (var decryptedStream = new MemoryStream())
      {
        using (var decryptor = rijCrypto.CreateDecryptor())
        using (var encryptedStream = new MemoryStream(_encryptedData))
        using (var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read, true))
          await cryptoStream.CopyToAsync(decryptedStream, 80 * 1024, _ct);

        return decryptedStream.ToArray();
      }
    }
  }

  public static byte[] CalculateSHAHash(byte[] _data, HashComplexity _hashComplexity = HashComplexity.Bit512)
  {
    var hashInst = _hashComplexity switch
    {
      HashComplexity.Bit256 => SHA256.Create() as HashAlgorithm,
      HashComplexity.Bit384 => SHA384.Create(),
      HashComplexity.Bit512 => SHA512.Create(),
      _ => throw new NotImplementedException()
    };

    using (hashInst)
      return hashInst.ComputeHash(_data);
  }

  public static byte[] CalculateSHAHash(Stream _stream, HashComplexity _hashComplexity = HashComplexity.Bit512)
  {
    var hashInst = _hashComplexity switch
    {
      HashComplexity.Bit256 => SHA256.Create() as HashAlgorithm,
      HashComplexity.Bit384 => SHA384.Create(),
      HashComplexity.Bit512 => SHA512.Create(),
      _ => throw new NotImplementedException()
    };

    return hashInst.ComputeHash(_stream);
  }

  public static string CalculateSHAHash(string _data, HashComplexity _hashComplexity = HashComplexity.Bit512)
  {
    var data = Encoding.UTF8.GetBytes(_data);
    var hash = CalculateSHAHash(data, _hashComplexity);
    return BitConverter.ToString(hash).Replace("-", "");
  }

  public static byte[] CalculateMd5Hash(byte[] _data)
  {
    using (var hash = MD5.Create())
      return hash.ComputeHash(_data);
  }

  public static string CalculateMd5Hash(string _data)
  {
    var data = Encoding.UTF8.GetBytes(_data);
    var hash = CalculateMd5Hash(data);
    return BitConverter.ToString(hash).Replace("-", "");
  }

}
