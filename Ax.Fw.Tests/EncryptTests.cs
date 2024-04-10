using Ax.Fw.Crypto;
using Ax.Fw.SharedTypes.Data.Crypto;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class EncryptTests
{
  private readonly ITestOutputHelper p_output;

  public EncryptTests(ITestOutputHelper output)
  {
    p_output = output;
  }

  [Theory(Timeout = 30000)]
  [InlineData(true)]
  [InlineData(false)]
  public async Task EncryptDecryptStreamAsync(bool _useFastHashing)
  {
    var lifetime = new Lifetime();
    try
    {
      var data = new byte[10 * 1024];
      Random.Shared.NextBytes(data);

      var password = Encoding.UTF8.GetBytes("123asd456qwe789zxc");

      using (var rawMs = new MemoryStream(data))
      using (var encryptedMs = new MemoryStream())
      {
        await AesCbc.EncryptAsync(rawMs, encryptedMs, password, _useFastHashing, lifetime.Token);

        encryptedMs.Position = 0;
        var encryptedData = encryptedMs.ToArray();

        Assert.NotEmpty(encryptedData);
        Assert.NotEqual(data, encryptedData);

        using (var decryptedMs = new MemoryStream())
        {
          await AesCbc.DecryptAsync(encryptedMs, decryptedMs, password, _useFastHashing, lifetime.Token);
          var decryptedData = decryptedMs.ToArray();

          Assert.NotEmpty(decryptedData);
          Assert.Equal(data, decryptedData);
        }
      }
    }
    finally
    {
      lifetime.End();
    }
  }

  [Theory(Timeout = 30000)]
  [InlineData(true, 10 * 1024)]
  [InlineData(true, 1024 * 1024)]
  [InlineData(true, 1165217)]
  [InlineData(false, 10 * 1024)]
  [InlineData(false, 1024 * 1024)]
  [InlineData(false, 1165217)]
  public async Task EncryptDecryptArrayAsync(bool _useFastHashing, int _size)
  {
    var lifetime = new Lifetime();
    try
    {
      var data = new byte[_size];
      Random.Shared.NextBytes(data);

      var password = Encoding.UTF8.GetBytes("123asd456qwe789zxc");

      var encryptedBytes = await AesCbc.EncryptAsync(data, password, _useFastHashing, lifetime.Token);

      Assert.NotEmpty(encryptedBytes);
      Assert.NotEqual(data, encryptedBytes);

      var decryptedBytes = await AesCbc.DecryptAsync(encryptedBytes, password, _useFastHashing, lifetime.Token);

      Assert.NotEmpty(decryptedBytes);
      Assert.Equal(data, decryptedBytes);
    }
    finally
    {
      lifetime.End();
    }
  }

  [Theory(Timeout = 30000)]
  [InlineData(true)]
  [InlineData(false)]
  public async Task IncorrectPasswordAsync(bool _useFastHashing)
  {
    var lifetime = new Lifetime();
    try
    {
      var data = new byte[10 * 1024];
      Random.Shared.NextBytes(data);

      var password = Encoding.UTF8.GetBytes("123asd456qwe789zxc");

      using (var rawMs = new MemoryStream(data))
      using (var encryptedMs = new MemoryStream())
      {
        await AesCbc.EncryptAsync(rawMs, encryptedMs, password, _useFastHashing, lifetime.Token);

        encryptedMs.Position = 0;
        var encryptedData = encryptedMs.ToArray();

        Assert.NotEmpty(encryptedData);
        Assert.NotEqual(data, encryptedData);

        using (var decryptedMs = new MemoryStream())
        {
          try
          {
            await AesCbc.DecryptAsync(encryptedMs, decryptedMs, password.Take(password.Length - 1).ToArray(), _useFastHashing, lifetime.Token);
            decryptedMs.Position = 0;
            var decryptedData = decryptedMs.ToArray();
            Assert.NotEqual(data, decryptedData);
          }
          catch (CryptographicException) { }
          catch
          {
            Assert.Fail("Seems like this universe is fucked up");
          }
        }

        password[^1] = 255;
        using (var decryptedMs = new MemoryStream())
        {
          await AesCbc.DecryptAsync(encryptedMs, decryptedMs, password, _useFastHashing, lifetime.Token);

          var decryptedData = decryptedMs.ToArray();
          Assert.NotEqual(data, decryptedData);
        }
      }
    }
    finally
    {
      lifetime.End();
    }
  }

  [Theory(Timeout = 30000)]
  [InlineData(512)]
  [InlineData(10 * 1024)]
  [InlineData(1024 * 1024)]
  [InlineData(1165217)]
  public void Chacha20Poly1305SimpleTest(int _size)
  {
    if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build < 20142)
      return;

    using var lifetime = new Lifetime();
    var key = Utilities.GetRandomString(8, false);
    var data = new byte[_size];
    Random.Shared.NextBytes(data);

    var chacha = lifetime.ToDisposeOnEnding(new ChaCha20WithPoly1305(key));

    var encryptedData = chacha.Encrypt(data);
    var encryptedDataBytes = encryptedData.ToArray();
    Assert.NotEmpty(encryptedData.ToArray());
    Assert.NotEqual(data, encryptedDataBytes);

    var decryptedData = chacha.Decrypt(encryptedData);
    var decryptedDataBytes = decryptedData.ToArray();
    Assert.NotEmpty(decryptedDataBytes);
    Assert.Equal(data, decryptedDataBytes);
  }

  [Theory(Timeout = 30000)]
  [InlineData(EncryptionKeyLength.Bits128, 512)]
  [InlineData(EncryptionKeyLength.Bits128, 1024 * 1024)]
  [InlineData(EncryptionKeyLength.Bits128, 1165217)]
  [InlineData(EncryptionKeyLength.Bits128, 11652170)]
  [InlineData(EncryptionKeyLength.Bits256, 512)]
  [InlineData(EncryptionKeyLength.Bits256, 1024 * 1024)]
  [InlineData(EncryptionKeyLength.Bits256, 1165217)]
  [InlineData(EncryptionKeyLength.Bits256, 11652170)]
  public void AesGcmSimpleTest(EncryptionKeyLength _keySize, int _taskSize)
  {
    using var lifetime = new Lifetime();
    var key = Utilities.GetRandomString(8, false);
    var data = new byte[_taskSize];
    Random.Shared.NextBytes(data);

    var aesGcm = lifetime.ToDisposeOnEnding(new AesWithGcm(key, _keySize));
    var sw = Stopwatch.StartNew();
    var elapsed = 0L;

    sw.Restart();
    var encryptedData = aesGcm.Encrypt(data);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"Encrypt: {elapsed}ms");
    Console.WriteLine($"Encrypt: {elapsed}ms");

    var encryptedDataBytes = encryptedData.ToArray();
    Assert.NotEmpty(encryptedData.ToArray());
    Assert.NotEqual(data, encryptedDataBytes);

    sw.Restart();
    var decryptedData = aesGcm.Decrypt(encryptedData);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"Decrypt: {elapsed}ms");
    Console.WriteLine($"Decrypt: {elapsed}ms");

    var decryptedDataBytes = decryptedData.ToArray();
    Assert.NotEmpty(decryptedDataBytes);
    Assert.Equal(data, decryptedDataBytes);
  }

  [Theory(Timeout = 30000)]
  [InlineData(EncryptionKeyLength.Bits128, 512, 80)]
  [InlineData(EncryptionKeyLength.Bits128, 512, 800)]
  [InlineData(EncryptionKeyLength.Bits128, 1165217, 800)]
  [InlineData(EncryptionKeyLength.Bits128, 1165217, 2165217)]
  [InlineData(EncryptionKeyLength.Bits256, 512, 80)]
  [InlineData(EncryptionKeyLength.Bits256, 512, 800)]
  [InlineData(EncryptionKeyLength.Bits256, 1165217, 800)]
  [InlineData(EncryptionKeyLength.Bits256, 1165217, 2165217)]
  public void AesGcmObfsSimpleTest(EncryptionKeyLength _keySize, int _taskSize, int _minChunkSize)
  {
    using var lifetime = new Lifetime();
    var key = Utilities.GetRandomString(8, false);
    var data = new byte[_taskSize];
    Random.Shared.NextBytes(data);

    var encryptedSize = data.Length + 4 + 4 + AesGcm.NonceByteSizes.MaxSize + 4 + AesGcm.TagByteSizes.MaxSize;

    var aesGcm = lifetime.ToDisposeOnEnding(new AesWithGcmObfs(key, _minChunkSize, _keySize));
    var sw = Stopwatch.StartNew();
    var elapsed = 0L;

    sw.Restart();
    var encryptedData = aesGcm.Encrypt(data);
    elapsed = sw.ElapsedTicks;
    p_output.WriteLine($"Encrypt: {elapsed} ticks");
    Console.WriteLine($"Encrypt: {elapsed} ticks");

    var encryptedDataBytes = encryptedData.ToArray();
    Assert.NotEmpty(encryptedData.ToArray());
    Assert.NotEqual(data, encryptedDataBytes);
    Assert.Equal(Math.Max(encryptedSize, _minChunkSize), encryptedDataBytes.Length);
    Assert.NotEqual(encryptedSize - 4, BinaryPrimitives.ReadInt32LittleEndian(encryptedDataBytes));

    sw.Restart();
    var decryptedData = aesGcm.Decrypt(encryptedData);
    elapsed = sw.ElapsedTicks;
    p_output.WriteLine($"Decrypt: {elapsed} ticks");
    Console.WriteLine($"Decrypt: {elapsed} ticks");

    var decryptedDataBytes = decryptedData.ToArray();
    Assert.NotEmpty(decryptedDataBytes);
    Assert.Equal(data, decryptedDataBytes);
    Assert.Equal(data.Length, decryptedDataBytes.Length);
  }

  [Theory(Timeout = 30000)]
  [InlineData(EncryptionKeyLength.Bits128, 512)]
  [InlineData(EncryptionKeyLength.Bits128, 1024 * 1024)]
  [InlineData(EncryptionKeyLength.Bits128, 1165217)]
  [InlineData(EncryptionKeyLength.Bits256, 512)]
  [InlineData(EncryptionKeyLength.Bits256, 1024 * 1024)]
  [InlineData(EncryptionKeyLength.Bits256, 1165217)]
  public void AesCbcTest(EncryptionKeyLength _keySize, int _taskSize)
  {
    using var lifetime = new Lifetime();
    var key = Utilities.GetRandomString(8, false);
    var data = new byte[_taskSize];
    Random.Shared.NextBytes(data);

    var aes = lifetime.ToDisposeOnEnding(new AesCbc(key, _keySize));
    var sw = Stopwatch.StartNew();
    var elapsed = 0L;

    var encryptedData = aes.Encrypt(data);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"Encrypt: {elapsed}ms");
    Console.WriteLine($"Encrypt: {elapsed}ms");

    var encryptedDataBytes = encryptedData.ToArray();
    Assert.NotEmpty(encryptedData.ToArray());
    Assert.NotEqual(data, encryptedDataBytes);

    var decryptedData = aes.Decrypt(encryptedData);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"Decrypt: {elapsed}ms");
    Console.WriteLine($"Decrypt: {elapsed}ms");

    var decryptedDataBytes = decryptedData.ToArray();
    Assert.NotEmpty(decryptedDataBytes);
    Assert.Equal(data, decryptedDataBytes);
  }

  [Fact(Timeout = 30000)]
  public void AesGcmStreamTest()
  {
    var key = Encoding.UTF8.GetBytes("fsdg54v26h4v35h4v2f68yb426");
    var data = new byte[10 * 1024 * 1024 - 6547];
    var sw = Stopwatch.StartNew();

    using var msRaw = new MemoryStream(data);
    using var msEnc = new MemoryStream();

    sw.Restart();
    AesWithGcm.EncryptStream(msRaw, msEnc, key);
    p_output.WriteLine($"AesGcm: encoding took {sw.ElapsedMilliseconds} ms");
    Assert.True(msEnc.Length > msRaw.Length);

    msEnc.Position = 0;
    using var msDec = new MemoryStream();
    sw.Restart();
    AesWithGcm.DecryptStream(msEnc, msDec, key);
    p_output.WriteLine($"AesGcm: decoding took {sw.ElapsedMilliseconds} ms");

    Assert.Equal(msDec.Length, data.Length);
    Assert.Equal(msDec.ToArray(), data);
  }

  [Fact(Timeout = 30000)]
  public void ChaCha20WithPoly1305StreamTest()
  {
    if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build < 20142)
      return;

    var key = Encoding.UTF8.GetBytes("fsdg54v26h4v35h4v2f68yb426");
    var data = new byte[10 * 1024 * 1024 - 6547];
    var sw = Stopwatch.StartNew();

    using var msRaw = new MemoryStream(data);
    using var msEnc = new MemoryStream();

    sw.Restart();
    ChaCha20WithPoly1305.EncryptStream(msRaw, msEnc, key);
    p_output.WriteLine($"ChaChaPoly1305: encoding took {sw.ElapsedMilliseconds} ms");
    Assert.True(msEnc.Length > msRaw.Length);

    msEnc.Position = 0;
    using var msDec = new MemoryStream();
    sw.Restart();
    ChaCha20WithPoly1305.DecryptStream(msEnc, msDec, key);
    p_output.WriteLine($"ChaChaPoly1305: decoding took {sw.ElapsedMilliseconds} ms");

    Assert.Equal(msDec.Length, data.Length);
    Assert.Equal(msDec.ToArray(), data);
  }

  [Theory(Timeout = 30000)]
  [InlineData(true)]
  [InlineData(false)]
  public async Task AesStreamTestAsync(bool _fastHashing)
  {
    var key = Encoding.UTF8.GetBytes("fsdg54v26h4v35h4v2f68yb426");
    var data = new byte[10 * 1024 * 1024 - 6547];
    var sw = Stopwatch.StartNew();

    using var msRaw = new MemoryStream(data);
    using var msEnc = new MemoryStream();

    sw.Restart();
    await AesCbc.EncryptAsync(msRaw, msEnc, key, _fastHashing);
    p_output.WriteLine($"AesCbc: encoding (fast hashing: {_fastHashing}) took {sw.ElapsedMilliseconds} ms");
    Assert.True(msEnc.Length > msRaw.Length);

    msEnc.Position = 0;
    using var msDec = new MemoryStream();
    sw.Restart();
    await AesCbc.DecryptAsync(msEnc, msDec, key, _fastHashing);
    p_output.WriteLine($"AesCbc: decoding (fast hashing: {_fastHashing}) took {sw.ElapsedMilliseconds} ms");

    Assert.Equal(msDec.Length, data.Length);
    Assert.Equal(msDec.ToArray(), data);
  }

  [Theory(Timeout = 30000)]
  [InlineData(512)]
  [InlineData(1024 * 1024)]
  [InlineData(1165217)]
  [InlineData(11652170)]
  public void XorSimpleTest(int _taskSize)
  {
    var key = Encoding.UTF8.GetBytes(Utilities.GetRandomString(8, false));
    var data = new byte[_taskSize];
    Random.Shared.NextBytes(data);

    var xor = new Xor(key);
    var sw = Stopwatch.StartNew();
    var elapsed = 0L;

    sw.Restart();
    var encryptedData = xor.Encrypt(data);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"Encrypt: {elapsed}ms");
    Console.WriteLine($"Encrypt: {elapsed}ms");

    var encryptedDataBytes = encryptedData.ToArray();
    Assert.NotEmpty(encryptedData.ToArray());
    Assert.NotEqual(data, encryptedDataBytes);

    sw.Restart();
    var decryptedData = xor.Decrypt(encryptedData);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"Decrypt: {elapsed}ms");
    Console.WriteLine($"Decrypt: {elapsed}ms");

    var decryptedDataBytes = decryptedData.ToArray();
    Assert.NotEmpty(decryptedDataBytes);
    Assert.Equal(data, decryptedDataBytes);
  }

  [Fact]
  public void XorInvalidHeaderTest()
  {
    var key = Encoding.UTF8.GetBytes(Utilities.GetRandomString(8, false));
    var data = new byte[32 * 1024];
    Random.Shared.NextBytes(data);

    var xor = new Xor(key);
    var encryptedData = xor.Encrypt(data);
    BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(0, 4), Random.Shared.Next());
    var encryptedDataBytes = encryptedData.ToArray();

    var ex = Assert.Throws<CryptographicException>(() => xor.Decrypt(encryptedDataBytes));
    Assert.Equal("Can't decrypt message - header is invalid", ex.Message);
  }

  [Theory(Timeout = 30000)]
  [InlineData(512)]
  [InlineData(1024 * 1024)]
  [InlineData(1165217)]
  [InlineData(11652170)]
  public void RsaAesSimpleTest(int _taskSize)
  {
    var data = new byte[_taskSize];
    Random.Shared.NextBytes(data);

    var rsaAes = new RsaAesGcm(
      GetRsaPrivateKey().ToCharArray(), 
      GetRsaPublicKey().ToCharArray(), 
      EncryptionKeyLength.Bits256);

    var sw = Stopwatch.StartNew();
    var elapsed = 0L;

    sw.Restart();
    var encryptedData = rsaAes.Encrypt(data);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"RsaAes Encrypt: {elapsed}ms");
    Console.WriteLine($"RsaAes Encrypt: {elapsed}ms");

    var encryptedDataBytes = encryptedData.ToArray();
    Assert.NotEmpty(encryptedData.ToArray());
    Assert.NotEqual(data, encryptedDataBytes);

    sw.Restart();
    var decryptedData = rsaAes.Decrypt(encryptedData);
    elapsed = sw.ElapsedMilliseconds;
    p_output.WriteLine($"RsaAes Decrypt: {elapsed}ms");
    Console.WriteLine($"RsaAes Decrypt: {elapsed}ms");

    var decryptedDataBytes = decryptedData.ToArray();
    Assert.NotEmpty(decryptedDataBytes);
    Assert.Equal(data, decryptedDataBytes);
  }

  private static string GetRsaPublicKey()
  {
    return "-----BEGIN PUBLIC KEY-----\r\nMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA8jMlwzPn0a1YsaWryiq8\r\nOh2Wn6EkF9wPIQPV7Vd+yR754+vF8L3yQAE+slKcRGXJtWuLrLN4MIsR0YKNRes3\r\ne2XnIGuZNGJ5+JD1IXu27k/H/Q9FGJKTO1SQ+2RAIPJzV5UuswW9oDIXXD2Sdi1p\r\nsM0d3Pn46/w+wknH/jIujB9+8MQCx+BPcpOAZcJtdfxab8reZ2JqwSEmq9U6Dakb\r\nYTfH/M62Zmw85/Gz5LC1LuMeB6SUSRM8wUy54Ps7wJ4uHYtlz0hbV1JFGbLgEtBF\r\n03RcTOw2qYvdxgPfz3I7T0HlWO6kObN4Yjc9Oud6LJF1mTeJeaawb27iIWjA0e5B\r\ntitBXiubiICJja8FF0oygrRFH4/U8T7IkATdeFRVrJ1dRhnG6mqbsFGb9dgjXizt\r\nONq3teryUR8Td9FLbVD1Vy5pCkIX1Gox+YWzzCePsZT6o9ZlLoZNS1qpgNVoNP56\r\nkIvut34lDx77esyZRba5y+Zma2tcWB5xDzqWsJ48u9wZAtVrBmJe0LPR2K9AzvMC\r\nVIBHFzei6Ayu/gOOuO+3nTf13dnvNm1SS+CTubMmxBGEKDGo1TnoNi7Tj/v9I5Zy\r\nJXfZrlt1/0IJu3oqFvUiY09nTiLaImFw9R/2Ke0V/0gfxJXlfOGl294NdaLsPYFj\r\nUH8LaiVJ9lgl59kSrbQgQYUCAwEAAQ==\r\n-----END PUBLIC KEY-----\r\n";
  }

  private static string GetRsaPrivateKey()
  {
    return "-----BEGIN RSA PRIVATE KEY-----\r\n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\r\n-----END RSA PRIVATE KEY-----\r\n";
  }

}
