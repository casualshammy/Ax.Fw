#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.Rnd;
using Ax.Fw.SharedTypes.Data;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.Workers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

    private static async Task CompressParallelGzipAsync(
        Stream _inputStream,
        Stream _outputStream,
        int _threads,
        CancellationToken _ct)
    {
        if (!_outputStream.CanSeek)
            throw new ArgumentException($"'{nameof(_outputStream)}' must be seekable!");
        if (!_outputStream.CanWrite)
            throw new ArgumentException($"'{nameof(_outputStream)}' must be writable!");

        var lifetime = new Lifetime();
        var chunkSize = 1024 * 1024;
        if (_inputStream.Length <= chunkSize)
        {
            using (var compressStream = new GZipStream(_outputStream, CompressionMode.Compress, true))
                await _inputStream.CopyToAsync(compressStream, 80 * 1024, _ct);

            return;
        }

        var headerSize = "AXGZIP".Length + sizeof(long) + sizeof(long); // mark + number of files + offset of footer 

        var offsetDictionary = new ConcurrentDictionary<long, string>();
        var workFlow = lifetime.DisposeOnCompleted(new Subject<byte[]>());
        var resultDictionary = new ConcurrentDictionary<long, byte[]>();
        var resultCursor = 0;
        var resultOffset = 0;
        var chunksCount = (int)Math.Ceiling(_inputStream.Length / (float)chunkSize);

        var team = WorkerTeam.Run(
            workFlow,
            async _ctx =>
            {
                return await CompressGzipBlock(_ctx.JobInfo.Job);
            },
            _failCtx =>
            {
                if (_failCtx.FailedCounter < 3)
                    return Task.FromResult(new PenaltyInfo(true, TimeSpan.FromMilliseconds(100)));
                else
                {
                    lifetime.Complete();
                    return Task.FromResult(new PenaltyInfo(false, null));
                }
            }, lifetime, _threads);

        team.CompletedJobs
            .Subscribe(_x =>
            {
                if (_x.Result == null || _x.Result.Length == 0)
                    throw new InvalidOperationException($"Can't compress a chunk of data!");

                if (_x.JobIndex == resultCursor)
                {
                    _outputStream.Seek(resultOffset, SeekOrigin.Begin);
                    _outputStream.Write(_x.Result, 0, _x.Result.Length);
                    resultCursor++;
                    resultOffset += _x.Result.Length;
                }
                else
                {
                    resultDictionary.TryAdd(_x.JobIndex, _x.Result);
                }

                while (resultDictionary.TryGetValue(resultCursor, out var savedChunk))
                {
                    _outputStream.Seek(resultOffset, SeekOrigin.Begin);
                    _outputStream.Write(savedChunk, 0, savedChunk.Length);
                    resultCursor++;
                    resultOffset += savedChunk.Length;
                }

                if (resultCursor == chunksCount)
                {
                    workFlow.OnCompleted();
                }
            }, lifetime);

        while (!_ct.IsCancellationRequested)
        {
            var buffer = new byte[chunkSize];
            var bytesRead = await _inputStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
                break;

            if (bytesRead == buffer.Length)
                workFlow.OnNext(buffer);
            else
                workFlow.OnNext(buffer.Take(bytesRead).ToArray());
        }

        await workFlow.LastOrDefaultAsync();
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

    public static async Task CompressToGzippedJsonAsync<T>(T _serializableObject, Stream _outputStream, CancellationToken _ct)
    {
        if (!_outputStream.CanWrite)
            throw new ArgumentException($"Stream must be writable", nameof(_outputStream));

        var jsonString = JsonConvert.SerializeObject(_serializableObject);
        var rawString = Encoding.UTF8.GetBytes(jsonString);

        using (var sourceStream = new MemoryStream(rawString))
        using (var compression = new GZipStream(_outputStream, CompressionMode.Compress, true))
            await sourceStream.CopyToAsync(compression, 80192, _ct);
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

    private static async Task<byte[]> CompressGzipBlock(byte[] _data)
    {
        using (var output = new MemoryStream())
        {
            using (var compressStream = new GZipStream(output, CompressionMode.Compress))
                await compressStream.WriteAsync(_data, 0, _data.Length);

            return output.ToArray();
        }
    }

}
