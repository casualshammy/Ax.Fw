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
      Utilities.SharedRandom.NextBytes(data);

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
      Utilities.SharedRandom.NextBytes(data);

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
      Utilities.SharedRandom.NextBytes(data);

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

}
