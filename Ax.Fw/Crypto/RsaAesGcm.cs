using Ax.Fw.SharedTypes.Data.Crypto;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Ax.Fw.Crypto;

public class RsaAesGcm : DisposableStack, ICryptoAlgorithm
{
  private readonly RSACryptoServiceProvider p_rsa;
  private readonly char[]? p_privateKeyPem;
  private readonly char[]? p_publicKeyPem;
  private readonly EncryptionKeyLength p_keyLength;

  public RsaAesGcm(
    char[]? _privateKeyPem,
    char[]? _publicKeyPem,
    EncryptionKeyLength _aesKeyLength = EncryptionKeyLength.Bits256)
  {
    p_rsa = ToDisposeOnEnded(new RSACryptoServiceProvider());
    p_privateKeyPem = _privateKeyPem;
    p_publicKeyPem = _publicKeyPem;
    p_keyLength = _aesKeyLength;
  }

  public Span<byte> Encrypt(ReadOnlySpan<byte> _data)
  {
    if (!(p_publicKeyPem?.Length > 0))
      throw new InvalidOperationException("Public key is not set");

    var symmetricKey = RandomNumberGenerator.GetBytes(32);
    using var aesGcm = new AesWithGcm(symmetricKey, p_keyLength);
    var encData = aesGcm.Encrypt(_data);

    p_rsa.ImportFromPem(p_publicKeyPem);
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

    p_rsa.ImportFromPem(p_privateKeyPem);
    var keySizeBytes = p_rsa.KeySize / 8;

    var encSymmetricKey = _data[..keySizeBytes];
    var symmetricKey = p_rsa.Decrypt(encSymmetricKey, RSAEncryptionPadding.Pkcs1);

    using var aesGcm = new AesWithGcm(symmetricKey, p_keyLength);
    var result = aesGcm.Decrypt(_data[keySizeBytes..]);
    return result;
  }

}
