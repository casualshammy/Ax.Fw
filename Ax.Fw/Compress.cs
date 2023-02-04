using Ax.Fw.Extensions;
using Ax.Fw.Rnd;
using Ax.Fw.SharedTypes.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw;

public static class Compress
{
    public static async Task CompressDirectoryToZipFileAsync(
        string _directory,
        string _zipPath,
        Action<TypedProgress<FileSystemInfo>>? _progressReport,
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

    public static async Task CompressDirectoryToZipFileAsync(
        string _directory,
        Stream _outputStream,
        Action<TypedProgress<FileSystemInfo>>? _progressReport,
        CancellationToken _ct)
    {
        var directory = new DirectoryInfo(_directory);
        if (!directory.Exists)
            throw new DirectoryNotFoundException();

        var filesRelativePaths = directory
            .GetFiles("*.*", SearchOption.AllDirectories)
            .ToDictionary(_x => _x, _x => _x.FullName.Substring(directory.FullName.Length).TrimStart('\\', '/'));

        await CompressListOfFilesAsync(filesRelativePaths, _outputStream, _progressReport, _ct);
    }

    public static async Task CompressListOfFilesAsync(
        IReadOnlyDictionary<FileInfo, string> _realPathWithRelativePath,
        string _zipPath,
        Action<TypedProgress<FileSystemInfo>>? _progressReport,
        CancellationToken _ct)
    {
        var tmpFile = $"{_zipPath}-{ThreadSafeRandomProvider.GetThreadRandom().Next()}.zip";

        try
        {
            using (var zipToOpen = new FileStream(tmpFile, FileMode.Create))
                await CompressListOfFilesAsync(_realPathWithRelativePath, zipToOpen, _progressReport, _ct);

            if (File.Exists(_zipPath))
                File.Delete(_zipPath);

            File.Move(tmpFile, _zipPath);
        }
        finally
        {
            new FileInfo(tmpFile).TryDelete();
        }
    }

    public static async Task CompressListOfFilesAsync(
        IReadOnlyDictionary<FileInfo, string> _realPathWithRelativePath,
        Stream _outputStream,
        Action<TypedProgress<FileSystemInfo>>? _progressReport,
        CancellationToken _ct)
    {
        if (!_outputStream.CanWrite)
            throw new ArgumentException($"'{nameof(_outputStream)}' must be writable!");

        using (var archive = new ZipArchive(_outputStream, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            var filesSizeProcessed = 0L;
            var totalFilesSize = await Task.Run(() => _realPathWithRelativePath.Keys.Sum(_x => _x.Length));
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

                filesSizeProcessed += fileInfo.Length;
                _progressReport?.Invoke(new TypedProgress<FileSystemInfo>(filesSizeProcessed, totalFilesSize, fileInfo));
            }
        }
    }

    public static async Task DecompressZipFileAsync(
        string _outputDirectory,
        string _zipPath,
        Action<TypedProgress<FileSystemInfo>>? _progressReport,
        CancellationToken _ct)
    {
        if (!File.Exists(_zipPath))
            throw new FileNotFoundException();

        using (var zipToOpen = new FileStream(_zipPath, FileMode.Open))
            await DecompressZipFileAsync(_outputDirectory, zipToOpen, _progressReport, _ct);
    }

    public static async Task DecompressZipFileAsync(
        string _outputDirectory,
        Stream _inputStream,
        Action<TypedProgress<FileSystemInfo>>? _progressReport,
        CancellationToken _ct)
    {
        if (!_inputStream.CanRead)
            throw new ArgumentException($"Can't read from stream '{nameof(_inputStream)}'!");

        var directory = new DirectoryInfo(_outputDirectory);
        if (!directory.Exists)
            Directory.CreateDirectory(_outputDirectory);

        using (var archive = new ZipArchive(_inputStream, ZipArchiveMode.Read, true, Encoding.UTF8))
        {
            var processedSize = 0L;
            var totalSize = archive.Entries.Select(_x => _x.Length).Sum();
            foreach (var entry in archive.Entries)
            {
                _ct.ThrowIfCancellationRequested();
                var entryFullName = entry.FullName;
                var fileAbsolutePath = Path.Combine(_outputDirectory, entryFullName);

                if (entryFullName.EndsWith("/") || entryFullName.EndsWith("\\"))
                {
                    var fileSystemInfo = new DirectoryInfo(fileAbsolutePath);
                    if (!fileSystemInfo.Exists)
                        Directory.CreateDirectory(fileSystemInfo.FullName);

                    _progressReport?.Invoke(new TypedProgress<FileSystemInfo>(processedSize, totalSize, fileSystemInfo));
                    continue;
                }

                var fileInfo = new FileInfo(fileAbsolutePath);
                if (!fileInfo.Directory.Exists)
                    Directory.CreateDirectory(fileInfo.Directory.FullName);

                using (var entryStream = entry.Open())
                using (var file = File.Open(fileAbsolutePath, FileMode.Create, FileAccess.Write))
                    await entryStream.CopyToAsync(file, 81920, _ct);

                processedSize += entry.Length;
                _progressReport?.Invoke(new TypedProgress<FileSystemInfo>(processedSize, totalSize, fileInfo));
            }
        }
    }

    /// <summary>
    /// Serialize to JSON and then gzip to byte array
    /// </summary>
    public static async Task<byte[]> CompressToGzippedJsonAsync<T>(T _serializableObject, CancellationToken _ct)
    {
        using var ms = new MemoryStream();
        await CompressToGzippedJsonAsync(_serializableObject, ms, _ct);
        return ms.ToArray();
    }

    /// <summary>
    /// Serialize to JSON and then gzip to output stream
    /// </summary>
    public static async Task CompressToGzippedJsonAsync<T>(T _serializableObject, Stream _outputStream, CancellationToken _ct)
    {
        if (!_outputStream.CanWrite)
            throw new ArgumentException($"Stream must be writable", nameof(_outputStream));

        using (var sourceStream = new MemoryStream())
        {
            using (var sw = new StreamWriter(sourceStream, Encoding.UTF8, 80192, true))
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, _serializableObject);
            }

            sourceStream.Position = 0;

            using (var compression = new GZipStream(_outputStream, CompressionMode.Compress, true))
                await sourceStream.CopyToAsync(compression, 80192, _ct);
        }
    }

    /// <summary>
    /// Un-gzip JSON from <see cref="byte[]"/> and then deserialize
    /// </summary>
    public static async Task<T?> DecompressGzippedJsonAsync<T>(byte[] _compressedBytes, CancellationToken _ct)
    {
        using var ms = new MemoryStream(_compressedBytes);
        var result = await DecompressGzippedJsonAsync<T>(ms, _ct);
        return result;
    }

    /// <summary>
    /// Un-gzip JSON from <see cref="Stream"/> and then deserialize
    /// </summary>
    public static async Task<T?> DecompressGzippedJsonAsync<T>(Stream _compressedStream, CancellationToken _ct)
    {
        T? result;
        using (var targetStream = new MemoryStream())
        {
            using (var decompression = new GZipStream(_compressedStream, CompressionMode.Decompress, true))
                await decompression.CopyToAsync(targetStream, 80192, _ct);

            targetStream.Position = 0;

            using (var reader = new StreamReader(targetStream, Encoding.UTF8, false, 80192, true))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var serializer = new JsonSerializer();
                result = serializer.Deserialize<T>(jsonReader);
            }
        }
        return result;
    }

}
