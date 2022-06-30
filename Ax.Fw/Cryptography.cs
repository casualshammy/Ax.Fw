using Ax.Fw.SharedTypes.Data;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw
{
    public static class Cryptography
    {
        public static async Task EncryptAes(Stream _inStream, Stream _outEncryptedStream, byte[] _password, CancellationToken _ct)
        {
            if (!_outEncryptedStream.CanWrite)
                throw new NotSupportedException($"Can't write to '{nameof(_outEncryptedStream)}'!");
            if (!_inStream.CanRead)
                throw new NotSupportedException($"Can't read from '{nameof(_inStream)}'!");

            using (var rijCrypto = Aes.Create("AES"))
            {
                rijCrypto.KeySize = 256;
                rijCrypto.BlockSize = 128;
                var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
                rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
                rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
                rijCrypto.Mode = CipherMode.CBC;

                using (var encryptor = rijCrypto.CreateEncryptor())
                using (var cryptoStream = new CryptoStream(_outEncryptedStream, encryptor, CryptoStreamMode.Write, true))
                    await _inStream.CopyToAsync(cryptoStream, 80 * 1024, _ct);
            }
        }

        public static async Task DecryptAes(Stream _inEncryptedStream, Stream _outStream, byte[] _password, CancellationToken _ct)
        {
            if (!_outStream.CanWrite)
                throw new NotSupportedException($"Can't write to '{nameof(_outStream)}'!");
            if (!_inEncryptedStream.CanRead)
                throw new NotSupportedException($"Can't read from '{nameof(_inEncryptedStream)}'!");

            using (var rijCrypto = Aes.Create("AES"))
            {
                rijCrypto.KeySize = 256;
                rijCrypto.BlockSize = 128;
                var key = new Rfc2898DeriveBytes(_password, _password.Reverse().ToArray(), 1000);
                rijCrypto.Key = key.GetBytes(rijCrypto.KeySize / 8);
                rijCrypto.IV = key.GetBytes(rijCrypto.BlockSize / 8);
                rijCrypto.Mode = CipherMode.CBC;

                using (var decryptor = rijCrypto.CreateDecryptor())
                using (var cryptoStream = new CryptoStream(_inEncryptedStream, decryptor, CryptoStreamMode.Read, true))
                    await cryptoStream.CopyToAsync(_outStream, 80 * 1024, _ct);
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
}
