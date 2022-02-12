#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.Rnd;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw
{
    public class DeCompressProgress
    {
        public DeCompressProgress(double _progressPercent, FileInfo _lastProcessedFile)
        {
            ProgressPercent = _progressPercent;
            LastProcessedFile = _lastProcessedFile;
        }

        public double ProgressPercent { get; }
        public FileInfo LastProcessedFile { get; }
    }

    public static class Compress
    {
        public static async Task CompressDirectoryToZipFile(
            string _directory, 
            string _zipPath, 
            Action<DeCompressProgress>? _progressReport, 
            CancellationToken _ct)
        {
            var directory = new DirectoryInfo(_directory);
            if (!directory.Exists)
                throw new DirectoryNotFoundException();

            var filesRelativePaths = directory
                .GetFiles("*.*", SearchOption.AllDirectories)
                .ToDictionary(_x => _x, _x => _x.FullName.Substring(directory.FullName.Length).TrimStart('\\', '/'));

            await CompressListOfFilesAsync(filesRelativePaths, _zipPath, _progressReport, _ct);
        }

        public static async Task CompressListOfFilesAsync(
            IDictionary<FileInfo, string> _realPathWithRelativePath, 
            string _zipPath, 
            Action<DeCompressProgress>? _progressReport, 
            CancellationToken _ct)
        {
            var tmpFile = $"{_zipPath}-{ThreadSafeRandomProvider.GetThreadRandom().Next()}.zip";

            try
            {
                using (var zipToOpen = new FileStream(tmpFile, FileMode.Create))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create, true, Encoding.UTF8))
                    {
                        var filesProcessed = 0L;
                        var totalFiles = (double)_realPathWithRelativePath.Count;
                        foreach (var pair in _realPathWithRelativePath)
                        {
                            _ct.ThrowIfCancellationRequested();
                            var fileInfo = pair.Key;
                            var fileRelativePath = pair.Value;

                            if (!fileInfo.Exists)
                                continue;

                            var entry = archive.CreateEntry(fileRelativePath);
                            using (var entryStream = entry.Open())
                            using (var file = File.OpenRead(fileInfo.FullName))
                                await file.CopyToAsync(entryStream);

                            _progressReport?.Invoke(new DeCompressProgress(++filesProcessed / totalFiles * 100, fileInfo));
                        }
                    }
                }

                if (File.Exists(_zipPath))
                    File.Delete(_zipPath);

                File.Move(tmpFile, _zipPath);
            }
            finally
            {
                new FileInfo(tmpFile).TryDelete();
            }
        }

        public static async Task DecompressZipFile(
            string _outputDirectory, 
            string _zipPath, 
            Action<DeCompressProgress>? _progressReport, 
            CancellationToken _ct)
        {
            if (!File.Exists(_zipPath))
                throw new FileNotFoundException();

            var directory = new DirectoryInfo(_outputDirectory);
            if (!directory.Exists)
                Directory.CreateDirectory(_outputDirectory);

            using (var zipToOpen = new FileStream(_zipPath, FileMode.Open))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.UTF8))
                {
                    var filesProcessed = 0L;
                    var totalFiles = (double)archive.Entries.Count;
                    foreach (var entry in archive.Entries)
                    {
                        _ct.ThrowIfCancellationRequested();
                        var fileAbsolutePath = Path.Combine(_outputDirectory, entry.FullName);
                        var fileInfo = new FileInfo(fileAbsolutePath);

                        if (!fileInfo.Directory.Exists)
                            Directory.CreateDirectory(fileInfo.Directory.FullName);

                        using (var entryStream = entry.Open())
                        using (var file = File.OpenWrite(fileAbsolutePath))
                            await entryStream.CopyToAsync(file);

                        _progressReport?.Invoke(new DeCompressProgress(++filesProcessed / totalFiles * 100, fileInfo));
                    }
                }
            }
        }

        /// <summary>
        /// Serialize to JSON and then gzip to byte array
        /// </summary>
        public static async Task<byte[]> CompressToGzippedJsonAsync<T>(T _serializableObject, CancellationToken _ct)
        {
            var jsonString = JsonConvert.SerializeObject(_serializableObject);
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
        public static async Task<T?> DecompressGzippedJsonAsync<T>(byte[] _compressedBytes, CancellationToken _ct)
        {
            byte[]? rawString = null;
            using (var sourceStream = new MemoryStream(_compressedBytes))
            {
                using (var targetStream = new MemoryStream(_compressedBytes.Length))
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
        public static async Task<T?> DecompressGzippedJsonAsync<T>(Stream _compressedStream, CancellationToken _ct)
        {
            byte[]? rawString = null;
            using (var targetStream = new MemoryStream())
            {
                using (var decompression = new GZipStream(_compressedStream, CompressionMode.Decompress, true))
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
