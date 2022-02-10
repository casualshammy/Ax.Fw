#nullable enable
using Ax.Fw.Workers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Ax.Fw.Extensions;

namespace Ax.Fw.Tests
{
    public class ZipFileTests
    {
        private readonly ITestOutputHelper p_output;

        public ZipFileTests(ITestOutputHelper output)
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
                void onProgress(double _percentCompleted, FileInfo _fileInfo)
                {
                    resultPercent = _percentCompleted;
                }

                await Compress.CompressDirectoryToZipFile(dir.FullName, tempFile, onProgress, lifetime.Token);

                Assert.InRange(resultPercent, 99d, 101d);

                resultPercent = 0d;
                var tempDirInfo = new DirectoryInfo(tempDir);
                if (!tempDirInfo.Exists)
                    Directory.CreateDirectory(tempDir);

                await Compress.DecompressZipFile(tempDir, tempFile, onProgress, lifetime.Token);

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
                void onProgress(double _percentCompleted, FileInfo _fileInfo)
                {
                    resultPercent = _percentCompleted;
                    if (resultPercent > 30)
                        lifetime.Complete();
                }

                await Assert.ThrowsAsync<OperationCanceledException>(async () => await Compress.CompressDirectoryToZipFile(dir.FullName, tempFile, onProgress, lifetime.Token));

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

    }
}
