using Ax.Fw.SharedTypes.Data.Crypto;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Ax.Fw.Crypto;

public class RsaAesGcm : DisposableStack, ICryptoAlgorithm
{
  private readonly RSACryptoServiceProvider p_rsa;
  private readonly string? p_publicKeyPem;
  private readonly string? p_privateKeyPem;
  private readonly byte[]? p_privateKeyPassword;
  private readonly EncryptionKeyLength p_keyLength;

  // openssl genrsa -out private_raw.pem 4096
  // openssl pkcs8 -topk8 -inform PEM -outform PEM -in private_raw.pem -out private.pem -passout pass:<your-password>
  // openssl rsa -in private.pem -outform PEM -pubout -out public.pem

  /// <summary>
  /// RSA + AES-GCM encryption
  /// </summary>
  /// <param name="_publicKeyPem">PEM-encoded public key (NOT a path to file!)</param>
  /// <param name="_privateKeyPem">PEM-encoded private key (NOT a path to file!)</param>
  /// <param name="_privateKeyPassword">Pay attention: only PKCS#8 encrypted private keys are supported</param>
  /// <param name="_aesKeyLength"></param>
  public RsaAesGcm(
    string? _publicKeyPem,
    string? _privateKeyPem,
    string? _privateKeyPassword,
    EncryptionKeyLength _aesKeyLength = EncryptionKeyLength.Bits256)
  {
    p_rsa = ToDisposeOnEnded(new RSACryptoServiceProvider());
    p_publicKeyPem = _publicKeyPem;
    p_privateKeyPem = _privateKeyPem;
    p_privateKeyPassword = _privateKeyPassword != null ? Encoding.UTF8.GetBytes(_privateKeyPassword) : null;
    p_keyLength = _aesKeyLength;
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data)
  {
    if (p_publicKeyPem == null && p_privateKeyPem == null)
      throw new InvalidOperationException("You must provide either public or private key to encrypt data");

    var symmetricKey = RandomNumberGenerator.GetBytes(32);
    using var aesGcm = new AesWithGcm(symmetricKey, p_keyLength);
    var encData = aesGcm.Encrypt(_data);

    if (p_publicKeyPem != null)
      p_rsa.ImportFromPem(p_publicKeyPem);
    else if (p_privateKeyPassword != null)
      p_rsa.ImportFromEncryptedPem(p_privateKeyPem, p_privateKeyPassword);
    else
      p_rsa.ImportFromPem(p_privateKeyPem);

    var encSymmetricKey = p_rsa.Encrypt(symmetricKey, RSAEncryptionPadding.Pkcs1);

    Span<byte> result = new byte[encSymmetricKey.Length + encData.Length];
    encSymmetricKey.CopyTo(result);
    encData.CopyTo(result[encSymmetricKey.Length..]);

    return result;
  }

  public Span<byte> Decrypt(ReadOnlySpan<byte> _data)
  {
    if (!(p_privateKeyPem?.Length > 0))
      throw new InvalidOperationException("Private key is not set");

    if (p_privateKeyPassword != null)
      p_rsa.ImportFromEncryptedPem(p_privateKeyPem, p_privateKeyPassword);
    else
      p_rsa.ImportFromPem(p_privateKeyPem);

    var keySizeBytes = p_rsa.KeySize / 8;

    var encSymmetricKey = _data[..keySizeBytes];
    var symmetricKey = p_rsa.Decrypt(encSymmetricKey, RSAEncryptionPadding.Pkcs1);

    using var aesGcm = new AesWithGcm(symmetricKey, p_keyLength);
    var result = aesGcm.Decrypt(_data[keySizeBytes..]);
    return result;
  }

}
