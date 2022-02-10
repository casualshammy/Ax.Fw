#nullable enable
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
    public static class Compress
    {
        public static async Task CompressDirectoryToZipFile(string _directory, string _zipPath, Action<double, FileInfo>? _progressReport, CancellationToken _ct)
        {
            var directory = new DirectoryInfo(_directory);
            if (!directory.Exists)
                throw new DirectoryNotFoundException();

            var filesRelativePaths = directory
                .GetFiles("*.*", SearchOption.AllDirectories)
                .ToDictionary(_x => _x, _x => _x.FullName.Substring(directory.FullName.Length).TrimStart('\\', '/'));

            using (var zipToOpen = new FileStream(_zipPath, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create, true, Encoding.UTF8))
                {
                    var filesProcessed = 0L;
                    var totalFiles = (double)filesRelativePaths.Count;
                    foreach (var pair in filesRelativePaths)
                    {
                        _ct.ThrowIfCancellationRequested();
                        var fileInfo = pair.Key;
                        var fileRelativePath = pair.Value;

                        var entry = archive.CreateEntry(fileRelativePath);
                        using (var entryStream = entry.Open())
                        using (var file = File.OpenRead(fileInfo.FullName))
                            await file.CopyToAsync(entryStream);

                        _progressReport?.Invoke(++filesProcessed / totalFiles * 100, fileInfo);
                    }
                }
            }
        }

        public static async Task CompressListOfFilesAsync(IDictionary<FileInfo, string> _realPathWithRelativePath, string _zipPath, Action<double, FileInfo>? _progressReport, CancellationToken _ct)
        {
            using (var zipToOpen = new FileStream(_zipPath, FileMode.Create))
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

                        _progressReport?.Invoke(++filesProcessed / totalFiles * 100, fileInfo);
                    }
                }
            }
        }

        public static async Task DecompressZipFile(string _outputDirectory, string _zipPath, Action<double, FileInfo>? _progressReport, CancellationToken _ct)
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

                        _progressReport?.Invoke(++filesProcessed / totalFiles * 100, fileInfo);
                    }
                }
            }
        }

    }
}
