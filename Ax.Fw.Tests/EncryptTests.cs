using Ax.Fw.Crypto;
using System;
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
        await Cryptography.EncryptAes(rawMs, encryptedMs, password, _useFastHashing, lifetime.Token);

        encryptedMs.Position = 0;
        var encryptedData = encryptedMs.ToArray();

        Assert.NotEmpty(encryptedData);
        Assert.NotEqual(data, encryptedData);

        using (var decryptedMs = new MemoryStream())
        {
          await Cryptography.DecryptAes(encryptedMs, decryptedMs, password, _useFastHashing, lifetime.Token);
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
      var data = new byte[10 * 1024];
      Utilities.SharedRandom.NextBytes(data);

      var password = Encoding.UTF8.GetBytes("123asd456qwe789zxc");

      var encryptedBytes = await Cryptography.EncryptAes(data, password, _useFastHashing, lifetime.Token);

      Assert.NotEmpty(encryptedBytes);
      Assert.NotEqual(data, encryptedBytes);

      var decryptedBytes = await Cryptography.DecryptAes(encryptedBytes, password, _useFastHashing, lifetime.Token);

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
        await Cryptography.EncryptAes(rawMs, encryptedMs, password, _useFastHashing, lifetime.Token);

        encryptedMs.Position = 0;
        var encryptedData = encryptedMs.ToArray();

        Assert.NotEmpty(encryptedData);
        Assert.NotEqual(data, encryptedData);

        using (var decryptedMs = new MemoryStream())
        {
          try
          {
            await Cryptography.DecryptAes(encryptedMs, decryptedMs, password.Take(password.Length - 1).ToArray(), _useFastHashing, lifetime.Token);
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
          await Cryptography.DecryptAes(encryptedMs, decryptedMs, password, _useFastHashing, lifetime.Token);

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

    var chacha = new ChaCha20WithPoly1305(lifetime, key);

    var encryptedData = chacha.Encrypt(data);
    Assert.NotEmpty(encryptedData);
    Assert.NotEqual(data, encryptedData);

    var decryptedData = chacha.Decrypt(encryptedData);
    Assert.NotEmpty(decryptedData);
    Assert.Equal(data, decryptedData);
  }

  [Theory(Timeout = 30000)]
  [InlineData(128, 512)]
  [InlineData(128, 1024 * 1024)]
  [InlineData(128, 1165217)]
  [InlineData(256, 512)]
  [InlineData(256, 1024 * 1024)]
  [InlineData(256, 1165217)]
  public void AesGcmSimpleTest(int _keySize, int _taskSize)
  {
    using var lifetime = new Lifetime();
    var key = Utilities.GetRandomString(8, false);
    var data = new byte[_taskSize];
    Random.Shared.NextBytes(data);

    var aesGcm = new AesWithGcm(lifetime, key, _keySize);

    var encryptedData = aesGcm.Encrypt(data);
    Assert.NotEmpty(encryptedData);
    Assert.NotEqual(data, encryptedData);

    var decryptedData = aesGcm.Decrypt(encryptedData);
    Assert.NotEmpty(decryptedData);
    Assert.Equal(data, decryptedData);
  }

  [Theory(Timeout = 30000)]
  [InlineData(128, 512)]
  [InlineData(128, 1024 * 1024)]
  [InlineData(128, 1165217)]
  [InlineData(256, 512)]
  [InlineData(256, 1024 * 1024)]
  [InlineData(256, 1165217)]
  public void AesCbcTest(int _keySize, int _taskSize)
  {
    using var lifetime = new Lifetime();
    var key = Utilities.GetRandomString(8, false);
    var data = new byte[_taskSize];
    Random.Shared.NextBytes(data);

    var aes = new AesCbc(lifetime, key, _keySize);

    var encryptedData = aes.Encrypt(data);
    Assert.NotEmpty(encryptedData);
    Assert.NotEqual(data, encryptedData);

    var decryptedData = aes.Decrypt(encryptedData);
    Assert.NotEmpty(decryptedData);
    Assert.Equal(data, decryptedData);
  }

}
