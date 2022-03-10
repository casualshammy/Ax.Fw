#nullable enable
using Ax.Fw.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class CompressTests
    {
        private readonly ITestOutputHelper p_output;

        public CompressTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact(Timeout = 30000)]
        public async Task BasicCompressDecompressAsync()
        {
            var lifetime = new Lifetime();
            var tempFile = Path.GetTempFileName();
            var tempDir = Path.Combine(Path.GetTempPath(), new Random().Next().ToString());
            try
            {
                var dir = new DirectoryInfo(Environment.CurrentDirectory);
                var md5 = dir.CreateMd5ForFolder();
                var size = dir.CalcDirectorySize();

                var resultPercent = 0d;
                void onProgress(DeCompressProgress _progress)
                {
                    resultPercent = _progress.ProgressPercent;
                }

                await Compress.CompressDirectoryToZipFileAsync(dir.FullName, tempFile, onProgress, lifetime.Token);

                Assert.InRange(resultPercent, 99d, 101d);

                resultPercent = 0d;
                var tempDirInfo = new DirectoryInfo(tempDir);
                if (!tempDirInfo.Exists)
                    Directory.CreateDirectory(tempDir);

                await Compress.DecompressZipFileAsync(tempDir, tempFile, onProgress, lifetime.Token);

                Assert.InRange(resultPercent, 99d, 101d);
                Assert.Equal(tempDirInfo.CreateMd5ForFolder(), md5);
                Assert.Equal(tempDirInfo.CalcDirectorySize(), size);
            }
            finally
            {
                lifetime.Complete();
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch { }
            }
        }

        [Fact(Timeout = 30000)]
        public async Task BasicCompressDecompressDictionaryAsync()
        {
            var lifetime = new Lifetime();
            var tempFile = Path.GetTempFileName();
            var tempDir = Path.Combine(Path.GetTempPath(), new Random().Next().ToString());
            try
            {
                var dir = new DirectoryInfo(Environment.CurrentDirectory);
                var files = dir
                    .GetFiles("*.*", SearchOption.AllDirectories)
                    .ToDictionary(_x => _x, _x => _x.FullName.Substring(dir.FullName.Length).TrimStart('\\', '/')); ;

                var md5 = dir.CreateMd5ForFolder();
                var size = dir.CalcDirectorySize();

                var resultPercent = 0d;
                void onProgress(DeCompressProgress _progress)
                {
                    resultPercent = _progress.ProgressPercent;
                }

                await Compress.CompressListOfFilesAsync(files, tempFile, onProgress, lifetime.Token);

                Assert.InRange(resultPercent, 99d, 101d);

                resultPercent = 0d;
                var tempDirInfo = new DirectoryInfo(tempDir);
                if (!tempDirInfo.Exists)
                    Directory.CreateDirectory(tempDir);

                await Compress.DecompressZipFileAsync(tempDir, tempFile, onProgress, lifetime.Token);

                Assert.InRange(resultPercent, 99d, 101d);
                Assert.Equal(tempDirInfo.CreateMd5ForFolder(), md5);
                Assert.Equal(tempDirInfo.CalcDirectorySize(), size);
            }
            finally
            {
                lifetime.Complete();
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch { }
            }
        }

        [Fact(Timeout = 30000)]
        public async Task InterruptedCompressAsync()
        {
            var lifetime = new Lifetime();
            var tempFile = Path.GetTempFileName();
            try
            {
                var dir = new DirectoryInfo(Environment.CurrentDirectory);

                var resultPercent = 0d;
                void onProgress(DeCompressProgress _progress)
                {
                    resultPercent = _progress.ProgressPercent;
                    if (resultPercent > 30)
                        lifetime.Complete();
                }

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await Compress.CompressDirectoryToZipFileAsync(dir.FullName, tempFile, onProgress, lifetime.Token));

                Assert.InRange(resultPercent, 30, 50);
            }
            finally
            {
                try
                {
                    lifetime.Complete();
                }
                catch { }
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
            }
        }

        [Fact(Timeout = 30000)]
        public async Task CompressDecompressOverwriteAsync()
        {
            var lifetime = new Lifetime();
            var tempFile = Path.GetTempFileName();
            var tempDir = Path.Combine(Path.GetTempPath(), new Random().Next().ToString());
            try
            {
                var dir = new DirectoryInfo(Environment.CurrentDirectory);
                var md5 = dir.CreateMd5ForFolder();
                var size = dir.CalcDirectorySize();

                var resultPercent = 0d;
                void onProgress(DeCompressProgress _progress)
                {
                    resultPercent = _progress.ProgressPercent;
                }

                await Compress.CompressDirectoryToZipFileAsync(dir.FullName, tempFile, onProgress, lifetime.Token);

                Assert.Equal(100d, resultPercent);

                resultPercent = 0d;
                var tempDirInfo = new DirectoryInfo(tempDir);
                if (!tempDirInfo.Exists)
                    Directory.CreateDirectory(tempDir);

                await Compress.DecompressZipFileAsync(tempDir, tempFile, onProgress, lifetime.Token);

                Assert.Equal(100d, resultPercent);
                Assert.Equal(tempDirInfo.CreateMd5ForFolder(), md5);
                Assert.Equal(tempDirInfo.CalcDirectorySize(), size);

                await Compress.DecompressZipFileAsync(tempDir, tempFile, onProgress, lifetime.Token);
                Assert.Equal(tempDirInfo.CreateMd5ForFolder(), md5);
                Assert.Equal(tempDirInfo.CalcDirectorySize(), size);
            }
            finally
            {
                lifetime.Complete();
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch { }
            }
        }

    }
}
