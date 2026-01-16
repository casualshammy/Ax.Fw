using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Tests.Tools;

namespace Ax.Fw.Storage.Tests;

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
#pragma warning disable IDE0060 // Remove unused parameter

public class SqliteBlobStorageTests
{
  [Theory]
  [Repeat(100)]
  public async Task Basic_Write_Read_Delete(int __)
  {
    const string ns = "default";
    const int key = 123;
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      _ = await storage.WriteDocumentAsync(ns, key, _data: data, lifetime.Token);

      using var outputStream0 = new MemoryStream();
      _ = await storage.ReadDocumentAsync(ns, key, outputStream0, lifetime.Token);

      outputStream0.Position = 0;
      Assert.Equal(data.Length, outputStream0.Length);
      Assert.Equal(data, outputStream0.ToArray());

      storage.DeleteDocuments(ns, key);

      using var outputStream1 = new MemoryStream();
      var data1 = await storage.ReadDocumentAsync(ns, key, outputStream1, lifetime.Token);

      Assert.Null(data1);
      Assert.Equal(0, outputStream1.Length);

      storage.Dispose();
      storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));

      var newData = GetData(1024 * 1024);
      _ = await storage.WriteDocumentAsync(ns, key, _data: newData, lifetime.Token);
      using var outputStream2 = new MemoryStream();
      _ = await storage.ReadDocumentAsync(ns, key, outputStream2, lifetime.Token);

      outputStream2.Position = 0;
      Assert.Equal(newData.Length, outputStream2.Length);
      Assert.Equal(newData, outputStream2.ToArray());
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
  public async Task Doc_Version_And_Dated_Are_Valid(int __)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      var doc0 = await storage.WriteDocumentAsync("default", 123, _data: data, lifetime.Token);
      var doc1 = storage.ListDocumentsMeta("default", new LikeExpr("123")).FirstOrDefault();

      Assert.Equal(doc0.Version, doc1?.Version);
      Assert.Equal(doc0.LastModified, doc1?.LastModified);
      Assert.Equal(doc0.Created, doc1?.Created);

      _ = await storage.WriteDocumentAsync("default", 123, _data: data, lifetime.Token);

      var doc2 = storage.ListDocumentsMeta("default", new LikeExpr("123")).FirstOrDefault();
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

  [Theory]
  [Repeat(100)]
  public async Task DocId_Is_Persist_During_Overwrites(int __)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data0 = GetData();
    var data1 = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));

      var record0 = await storage.WriteDocumentAsync("default", 123, _data: data0, lifetime.Token);
      using var outputStream0 = new MemoryStream();
      _ = await storage.ReadDocumentAsync("default", 123, outputStream0, lifetime.Token);

      _ = await storage.WriteDocumentAsync("default", 123, _data: data1, lifetime.Token);
      using var outputStream1 = new MemoryStream();
      var record2 = await storage.ReadDocumentAsync("default", 123, outputStream1, lifetime.Token);

      Assert.NotEqual(outputStream0.ToArray(), outputStream1.ToArray());
      Assert.Equal(data0, outputStream0.ToArray());
      Assert.Equal(data1, outputStream1.ToArray());
      Assert.Equal(record0.DocId, record2?.DocId);

      var list = storage.ListDocumentsMeta();
      Assert.Single(list);
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
  public async Task DocId_Is_Monotonic_In_Session(int __)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data = GetData();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));

      var record0 = await storage.WriteDocumentAsync("test-table", "test-key", data, lifetime.Token);
      Assert.Equal(0, record0.DocId);

      storage.DeleteDocuments("test-table", "test-key");

      var list0 = storage.ListDocumentsMeta("test-table");
      Assert.Empty(list0);

      var record1 = await storage.WriteDocumentAsync("test-table", "test-key", data, lifetime.Token);
      Assert.NotEqual(0, record1.DocId);
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
  public async Task DocId_Calculated_Correctly(int __)
  {
    var lifetime0 = new Lifetime();
    var lifetime1 = new Lifetime();
    var dbFile = GetDbTmpPath();
    var data0 = GetData();
    var data1 = GetData();
    try
    {
      // open db, write documents, then close db
      var entriesCount = 100;
      var storage0 = lifetime0.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      var enumerable = Enumerable.Range(0, entriesCount);

      var lastDocId = 0;
      await Parallel.ForEachAsync(enumerable, lifetime0.Token, async (_key, _c) =>
      {
        var document0 = await storage0.WriteDocumentAsync("test-table0", _key, data0, _c);
        var document1 = await storage0.WriteDocumentAsync("test-table1", _key, data0, _c);
        var document2 = await storage0.WriteDocumentAsync("test-table2", _key, data0, _c);

        lastDocId = Math.Max(lastDocId, document0.DocId);
        lastDocId = Math.Max(lastDocId, document1.DocId);
        lastDocId = Math.Max(lastDocId, document2.DocId);
      });

      lifetime0.End();

      Assert.Equal(entriesCount * 3, lastDocId + 1);

      var storage1 = lifetime1.ToDisposeOnEnding(new SqliteBlobStorage(dbFile));
      var document = await storage1.WriteDocumentAsync("test-table", entriesCount + 1, data1, lifetime1.Token);

      Assert.True(document.DocId > lastDocId);
    }
    finally
    {
      lifetime0.End();
      lifetime1.End();
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
        await storage.WriteDocumentAsync("default", i, data, lifetime.Token);
        Assert.Equal(i + 1, storage.Count("default", null));
        Assert.Equal(0, storage.Count(wrongNs, null));
      }

      for (int i = 0; i < 3; i++)
      {
        storage.DeleteDocuments("default", i);
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
        await storage.WriteDocumentAsync("default", i, data, lifetime.Token);

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
