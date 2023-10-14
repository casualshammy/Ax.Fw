using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Crypto;

public class ChaCha20WithPoly1305
{
  private readonly ICryptoTransform p_aesEncryptTransform;
  private readonly ICryptoTransform p_aesDecryptTransform;
  private readonly NaCl.Core.ChaCha20Poly1305 p_chacha;
  private readonly byte[] p_noncePrefix = new byte[4];
  private long p_nonce;

  public ChaCha20WithPoly1305(IReadOnlyLifetime _lifetime, string _key)
  {
    Utilities.SharedRandom.NextBytes(p_noncePrefix);

    var key = Encoding.UTF8.GetBytes(_key);
    using var sha = _lifetime.ToDisposeOnEnding(SHA512.Create());
    var hashSource = sha.ComputeHash(key);
    p_chacha = new(hashSource.Take(32).ToArray());

    p_aesEncryptTransform = _lifetime.ToDisposeOnEnding(CreateEncryptor(hashSource, _lifetime));
    p_aesDecryptTransform = _lifetime.ToDisposeOnEnding(CreateDecryptor(hashSource, _lifetime));
  }

  public async Task<byte[]> EncryptAsync(byte[] _data, CancellationToken _ct)
  {
    var nonce = p_noncePrefix
      .Concat(BitConverter.GetBytes(Interlocked.Increment(ref p_nonce)))
      .ToArray();

    var encryptedBytes = new byte[_data.Length];
    var tag = new byte[16];

    p_chacha.Encrypt(nonce, _data, encryptedBytes, tag);
    using (var ms = new MemoryStream(16 + 32 + encryptedBytes.Length))
    {
      await AesEncryptAsync(nonce.Concat(tag).ToArray(), ms, _ct);
      await ms.WriteAsync(encryptedBytes, _ct);
      await ms.FlushAsync(_ct);
      return ms.ToArray();
    }
  }

  public async Task<byte[]> DecryptAsync(byte[] _data, CancellationToken _ct)
  {
    var nonceAndTag = await AesDecryptAsync(_data[0..32], _ct);
    var dataLength = _data.Length - 32;

    var decryptedBytes = new byte[dataLength];
    p_chacha.Decrypt(nonceAndTag.AsSpan(0, 12), _data.AsSpan(32, dataLength), nonceAndTag.AsSpan(12, 16), decryptedBytes);
    return decryptedBytes;
  }

  private async Task AesEncryptAsync(byte[] _data, Stream _outputStream, CancellationToken _ct)
  {
    using (var rawMs = new MemoryStream(_data))
    using (var cryptoStream = new CryptoStream(_outputStream, p_aesEncryptTransform, CryptoStreamMode.Write, true))
      await rawMs.CopyToAsync(cryptoStream, 80 * 1024, _ct);
  }

  private async Task<byte[]> AesDecryptAsync(byte[] _data, CancellationToken _ct)
  {
    using (var decryptedStream = new MemoryStream())
    {
      using (var encryptedStream = new MemoryStream(_data))
      using (var cryptoStream = new CryptoStream(encryptedStream, p_aesDecryptTransform, CryptoStreamMode.Read, true))
        await cryptoStream.CopyToAsync(decryptedStream, 80 * 1024, _ct);

      return decryptedStream.ToArray();
    }
  }

  private static ICryptoTransform CreateEncryptor(byte[] _keyHash, IReadOnlyLifetime _lifetime)
  {
    var rijCrypto = _lifetime.ToDisposeOnEnding(Aes.Create());
    rijCrypto.KeySize = 256;
    rijCrypto.BlockSize = 128;
    rijCrypto.Key = _keyHash.Take(rijCrypto.KeySize / 8).ToArray();
    rijCrypto.IV = _keyHash.Reverse().Take(rijCrypto.BlockSize / 8).ToArray();
    rijCrypto.Mode = CipherMode.CBC;
    rijCrypto.Padding = PaddingMode.PKCS7;
    return rijCrypto.CreateEncryptor();
  }

  private static ICryptoTransform CreateDecryptor(byte[] _keyHash, IReadOnlyLifetime _lifetime)
  {
    var rijCrypto = _lifetime.ToDisposeOnEnding(Aes.Create());
    rijCrypto.KeySize = 256;
    rijCrypto.BlockSize = 128;
    rijCrypto.Key = _keyHash.Take(rijCrypto.KeySize / 8).ToArray();
    rijCrypto.IV = _keyHash.Reverse().Take(rijCrypto.BlockSize / 8).ToArray();
    rijCrypto.Mode = CipherMode.CBC;
    rijCrypto.Padding = PaddingMode.PKCS7;
    return rijCrypto.CreateDecryptor();
  }

}
