using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Tests.Tools;
using Xunit.Abstractions;

namespace Ax.Fw.Storage.Tests;

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
#pragma warning disable IDE0060 // Remove unused parameter

public class SqliteBlobStorageTests
{
  private readonly ITestOutputHelper p_output;

  public SqliteBlobStorageTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Theory]
  [Repeat(100)]
  public async Task Basic_Write_Read_Delete_Bytes(int __)
  {
    const string ns = "default";
    const int key = 123;
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      _ = await storage.WriteBlobAsync(ns, key, data, lifetime.Token);

      {
        if (!storage.TryReadBlob(ns, key, out byte[]? storedData, out var meta))
          Assert.Fail("Blob not found after write");

        Assert.NotNull(storedData);
        Assert.NotNull(meta);
        Assert.Equal(data.Length, meta.Length);
        Assert.Equal(data, storedData);
      }

      storage.DeleteBlobs(ns, key);

      {
        if (storage.TryReadBlob(ns, key, out byte[]? storedData, out var meta))
          Assert.Fail("Blob found after delete");

        Assert.Null(storedData);
        Assert.Null(meta);
      }
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Theory]
  [Repeat(100)]
  public async Task Basic_Write_Read_Delete_Stream(int __)
  {
    const string ns = "default";
    const int key = 123;
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = lifetime.ToDisposeOnEnded(new MemoryStream(GetData()));
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      _ = await storage.WriteBlobAsync(ns, key, data, data.Length, lifetime.Token);

      {
        if (!storage.TryReadBlob(ns, key, out BlobStream? storedData, out var meta))
          Assert.Fail("Blob not found after write");

        Assert.NotNull(storedData);
        Assert.NotNull(meta);
        Assert.Equal(data.Length, meta.Length);
        Assert.Equal(data.ToArray(), storedData.ToArray());
        storedData.Dispose();
      }

      storage.DeleteBlobs(ns, key);

      {
        if (storage.TryReadBlob(ns, key, out BlobStream? storedData, out var meta))
          Assert.Fail("Blob found after delete");

        Assert.Null(storedData);
        Assert.Null(meta);
      }
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task Parallel_Write_Stream()
  {
    const string ns = "default";
    const int key = 123;
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));

      await storage.WriteBlobAsync(ns, key, data, lifetime.Token);

      await Parallel.ForAsync(0, 10, lifetime.Token, async (__, _c) =>
      {
        if (!storage.TryReadBlob(ns, key, out BlobStream? blobStream, out _))
          Assert.Fail("Blob not found after write");

        try
        {
          using var ms = new MemoryStream();
          await blobStream.CopyToAsync(ms, lifetime.Token);
        }
        finally
        {
          blobStream.Dispose();
        }
      });
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task Write_Same_Key_Multiple_Times_Updates_Blob()
  {
    const string ns = "default";
    const int key = 123;
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data0 = GetData();
    var data1 = GetData();

    try
    {
      Assert.NotEqual(data0, data1);

      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));

      {
        _ = await storage.WriteBlobAsync(ns, key, data0, lifetime.Token);
        if (!storage.TryReadBlob(ns, key, out byte[]? storedData, out var meta))
          Assert.Fail("Blob not found after write");

        Assert.NotNull(storedData);
        Assert.NotNull(meta);
        Assert.Equal(data0, storedData);
      }

      {
        _ = await storage.WriteBlobAsync(ns, key, data1, lifetime.Token);
        if (!storage.TryReadBlob(ns, key, out byte[]? storedData, out var meta))
          Assert.Fail("Blob not found after write");

        Assert.NotNull(storedData);
        Assert.NotNull(meta);
        Assert.Equal(data1, storedData);
      }
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task Incorrect_Stream_Size()
  {
    const string ns = "default";
    const int key = 123;
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var dataBytes = GetData();
    var dataMs = lifetime.ToDisposeOnEnded(new MemoryStream(dataBytes));
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));

      {
        _ = await storage.WriteBlobAsync(ns, key, dataMs, dataBytes.Length - 1, lifetime.Token);

        if (!storage.TryReadBlob(ns, key, out BlobStream? storedData, out var meta))
          Assert.Fail("Blob not found after write");

        Assert.NotNull(storedData);
        Assert.NotNull(meta);
        Assert.Equal(dataBytes.Length - 1, meta.Length);
        Assert.Equal(dataBytes.AsMemory(0, dataBytes.Length - 1), storedData.ToArray());
        storedData.Dispose();
      }

      {
        await Assert.ThrowsAsync<EndOfStreamException>(() => storage.WriteBlobAsync(ns, key, dataMs, dataBytes.Length + 1, lifetime.Token));
      }
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task Doc_Version_And_Dates_Are_Correctly_Updated()
  {
    const string ns = "default";
    const int key = 123;
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      var doc0 = await storage.WriteBlobAsync(ns, key, data, lifetime.Token);
      var doc1 = storage.ListBlobsMeta(ns, new LikeExpr(key.ToString())).FirstOrDefault();

      Assert.Equal(doc0.Version, doc1?.Version);
      Assert.Equal(doc0.LastModified, doc1?.LastModified);
      Assert.Equal(doc0.Created, doc1?.Created);

      _ = await storage.WriteBlobAsync(ns, key, data, lifetime.Token);

      var doc2 = storage.ListBlobsMeta(ns, new LikeExpr(key.ToString())).FirstOrDefault();
      Assert.NotEqual(doc0.Version, doc2?.Version);
      Assert.NotEqual(doc0.LastModified, doc2?.LastModified);
      Assert.Equal(doc0.Created, doc2?.Created);
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task Count_Returns_Valid_Values()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var wrongNs = "wrong_ns";
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      Assert.Equal(0, storage.Count(wrongNs, null));

      for (int i = 0; i < 3; i++)
      {
        await storage.WriteBlobAsync("default", i, data, lifetime.Token);
        Assert.Equal(i + 1, storage.Count("default", null));
        Assert.Equal(0, storage.Count(wrongNs, null));
      }

      for (int i = 0; i < 3; i++)
      {
        storage.DeleteBlobs("default", i);
        Assert.Equal(2 - i, storage.Count("default", null));
        Assert.Equal(0, storage.Count(wrongNs, null));
      }
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task Flush_Feature_Shrinks_File()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      for (var i = 0; i < 1000; i++)
        await storage.WriteBlobAsync("default", i, data, lifetime.Token);

      var walFile = new FileInfo($"{dbFile}-wal");
      Assert.True(walFile.Exists);

      var origWalFileSize = walFile.Length;
      storage.Flush(false);
      walFile.Refresh();
      Assert.Equal(origWalFileSize, walFile.Length);

      storage.Flush(true);
      walFile.Refresh();
      Assert.Equal(0, walFile.Length);
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";

  private static byte[] GetData(int _size = 1024)
  {
    var buffer = new byte[_size];
    new Random().NextBytes(buffer);
    return buffer;
  }

}

#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
#pragma warning restore IDE0060 // Remove unused parameter
