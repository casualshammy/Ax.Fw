using Ax.Fw.Rnd;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class EncryptTests
    {
        private readonly ITestOutputHelper p_output;

        public EncryptTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact(Timeout = 30000)]
        public async Task EncryptDecryptAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var data = new byte[10 * 1024];
                ThreadSafeRandomProvider.GetThreadRandom().NextBytes(data);

                var password = Encoding.UTF8.GetBytes("123asd456qwe789zxc");

                using (var rawMs = new MemoryStream(data))
                using (var encryptedMs = new MemoryStream())
                {
                    await Cryptography.EncryptAes(rawMs, encryptedMs, password, lifetime.Token);

                    encryptedMs.Position = 0;
                    var encryptedData = encryptedMs.ToArray();

                    Assert.NotEmpty(encryptedData);
                    Assert.NotEqual(data, encryptedData);

                    using (var decryptedMs = new MemoryStream())
                    {
                        await Cryptography.DecryptAes(encryptedMs, decryptedMs, password, lifetime.Token);
                        var decryptedData = decryptedMs.ToArray();

                        Assert.NotEmpty(decryptedData);
                        Assert.Equal(data, decryptedData);
                    }
                }
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact(Timeout = 30000)]
        public async Task IncorrectPasswordAsync()
        {
            var lifetime = new Lifetime();
            try
            {
                var data = new byte[10 * 1024];
                ThreadSafeRandomProvider.GetThreadRandom().NextBytes(data);

                var password = Encoding.UTF8.GetBytes("123asd456qwe789zxc");

                using (var rawMs = new MemoryStream(data))
                using (var encryptedMs = new MemoryStream())
                {
                    await Cryptography.EncryptAes(rawMs, encryptedMs, password, lifetime.Token);

                    encryptedMs.Position = 0;
                    var encryptedData = encryptedMs.ToArray();

                    Assert.NotEmpty(encryptedData);
                    Assert.NotEqual(data, encryptedData);

                    using (var decryptedMs = new MemoryStream())
                        await Assert.ThrowsAsync<CryptographicException>(async () => await Cryptography.DecryptAes(encryptedMs, decryptedMs, password.Take(password.Length - 1).ToArray(), lifetime.Token));

                    password[^1] = 255;
                    using (var decryptedMs = new MemoryStream())
                    {
                        await Cryptography.DecryptAes(encryptedMs, decryptedMs, password, lifetime.Token);

                        var decryptedData = decryptedMs.ToArray();
                        Assert.NotEqual(data, decryptedData);
                    }
                }
            }
            finally
            {
                lifetime.Complete();
            }
        }

    }
}
