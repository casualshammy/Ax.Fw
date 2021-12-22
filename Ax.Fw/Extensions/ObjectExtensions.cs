#nullable enable
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Serialize to JSON and then gzip to byte array
        /// </summary>
        public static byte[] ToGzippedJson<T>(this T _this)
        {
            var jsonString = JsonConvert.SerializeObject(_this);
            var rawString = Encoding.UTF8.GetBytes(jsonString);

            using (var sourceStream = new MemoryStream(rawString))
            {
                using (var targetStream = new MemoryStream())
                {
                    using (var compression = new GZipStream(targetStream, CompressionMode.Compress, true))
                    {
                        sourceStream.CopyTo(compression);
                    }
                    return targetStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Serialize to JSON and then gzip to byte array
        /// </summary>
        public static async Task<byte[]> ToGzippedJsonAsync<T>(this T _this, CancellationToken _ct)
        {
            var jsonString = JsonConvert.SerializeObject(_this);
            var rawString = Encoding.UTF8.GetBytes(jsonString);

            using (var sourceStream = new MemoryStream(rawString))
            {
                using (var targetStream = new MemoryStream())
                {
                    using (var compression = new GZipStream(targetStream, CompressionMode.Compress, true))
                    {
                        await sourceStream.CopyToAsync(compression, 80192, _ct);
                    }
                    return targetStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Un-gzip JSON from <see cref="byte[]"/> and then deserialize
        /// </summary>
        public static T? FromGzippedJson<T>(this byte[] _this)
        {
            byte[]? rawString = null;
            using (var sourceStream = new MemoryStream(_this))
            {
                using (var targetStream = new MemoryStream(_this.Length))
                {
                    using (var decompression = new GZipStream(sourceStream, CompressionMode.Decompress, true))
                    {
                        decompression.CopyTo(targetStream);
                    }
                    rawString = targetStream.ToArray();
                }
            }

            var jsonString = Encoding.UTF8.GetString(rawString);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// Un-gzip JSON from <see cref="Stream"/> and then deserialize
        /// </summary>
        public static T? FromGzippedJson<T>(this Stream _this)
        {
            byte[]? rawString = null;
            using (var targetStream = new MemoryStream())
            {
                using (var decompression = new GZipStream(_this, CompressionMode.Decompress, true))
                {
                    decompression.CopyTo(targetStream);
                }
                rawString = targetStream.ToArray();
            }

            var jsonString = Encoding.UTF8.GetString(rawString);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// Un-gzip JSON from <see cref="byte[]"/> and then deserialize
        /// </summary>
        public static async Task<T?> FromGzippedJsonAsync<T>(this byte[] _this, CancellationToken _ct)
        {
            byte[]? rawString = null;
            using (var sourceStream = new MemoryStream(_this))
            {
                using (var targetStream = new MemoryStream(_this.Length))
                {
                    using (var decompression = new GZipStream(sourceStream, CompressionMode.Decompress, true))
                    {
                        await decompression.CopyToAsync(targetStream, 80192, _ct);
                    }
                    rawString = targetStream.ToArray();
                }
            }

            var jsonString = Encoding.UTF8.GetString(rawString);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// Un-gzip JSON from <see cref="Stream"/> and then deserialize
        /// </summary>
        public static async Task<T?> FromGzippedJsonAsync<T>(this Stream _this, CancellationToken _ct)
        {
            byte[]? rawString = null;
            using (var targetStream = new MemoryStream())
            {
                using (var decompression = new GZipStream(_this, CompressionMode.Decompress, true))
                {
                    await decompression.CopyToAsync(targetStream, 80192, _ct);
                }
                rawString = targetStream.ToArray();
            }

            var jsonString = Encoding.UTF8.GetString(rawString);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

    }
}
