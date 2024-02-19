using Ax.Fw.SharedTypes.Data.Crypto;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Crypto;

public class AesCbc : DisposableStack, ICryptoAlgorithm
{
  private readonly byte[] p_iv;
  private readonly Aes p_aes;

  public AesCbc(string _key, EncryptionKeyLength _keyLength = EncryptionKeyLength.Bits256)
  {
    if (_keyLength != EncryptionKeyLength.Bits128 && _keyLength != EncryptionKeyLength.Bits256)
      throw new ArgumentOutOfRangeException(nameof(_keyLength), $"Key length must be 128 or 256 bits");

    var key = Encoding.UTF8.GetBytes(_key);
    var hashSource = SHA512.HashData(key);

    p_aes = ToDispose(Aes.Create());
    p_aes.KeySize = (int)_keyLength;
    p_aes.BlockSize = 128;
    p_iv = hashSource
      .Reverse()
      .Take(p_aes.BlockSize / 8)
      .ToArray();
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data) => p_aes.EncryptCbc(_data, p_iv, PaddingMode.PKCS7);

  public Span<byte> Decrypt(ReadOnlySpan<byte> _data) => p_aes.DecryptCbc(_data, p_iv, PaddingMode.PKCS7);

  public static async Task EncryptAsync(
    Stream _inStream,
    Stream _outEncryptedStream,
    byte[] _password,
    bool _useFastHashing = false,
    CancellationToken _ct = default)
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
#pragma warning disable SYSLIB0041 // 型またはメンバーが旧型式です
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
#pragma warning restore SYSLIB0041 // 型またはメンバーが旧型式です
        rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
        rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
      }

      rijCrypto.Mode = CipherMode.CBC;

      using (var encryptor = rijCrypto.CreateEncryptor())
      using (var cryptoStream = new CryptoStream(_outEncryptedStream, encryptor, CryptoStreamMode.Write, true))
        await _inStream.CopyToAsync(cryptoStream, 80 * 1024, _ct);
    }
  }

  public static async Task<byte[]> EncryptAsync(
    byte[] _data,
    byte[] _password,
    bool _useFastHashing = false,
    CancellationToken _ct = default)
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
#pragma warning disable SYSLIB0041 // 型またはメンバーが旧型式です
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
#pragma warning restore SYSLIB0041 // 型またはメンバーが旧型式です
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

  public static async Task DecryptAsync(
    Stream _inEncryptedStream,
    Stream _outStream,
    byte[] _password,
    bool _useFastHashing = false,
    CancellationToken _ct = default)
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
#pragma warning disable SYSLIB0041 // 型またはメンバーが旧型式です
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
#pragma warning restore SYSLIB0041 // 型またはメンバーが旧型式です
        rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
        rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
      }

      rijCrypto.Mode = CipherMode.CBC;

      using (var decryptor = rijCrypto.CreateDecryptor())
      using (var cryptoStream = new CryptoStream(_inEncryptedStream, decryptor, CryptoStreamMode.Read, true))
        await cryptoStream.CopyToAsync(_outStream, 80 * 1024, _ct);
    }
  }

  public static async Task<byte[]> DecryptAsync(
    byte[] _encryptedData,
    byte[] _password,
    bool _useFastHashing = false,
    CancellationToken _ct = default)
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
#pragma warning disable SYSLIB0041 // 型またはメンバーが旧型式です
        using var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
#pragma warning restore SYSLIB0041 // 型またはメンバーが旧型式です
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

}