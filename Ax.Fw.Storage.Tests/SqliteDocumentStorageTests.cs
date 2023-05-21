using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Attributes;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Ax.Fw.Tests.Tools;
using System.Diagnostics;

namespace Ax.Fw.Storage.Tests;

public class SqliteDocumentStorageTests
{
  [SimpleDocument("simple-record")]
  record DataRecord(int Id, string Name)
  {
    public string GetStorageKey() => $"{Id}.{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    public static int? GetIdFromStorageKey(string _storageKey)
    {
      var split = _storageKey.Split('.', StringSplitOptions.RemoveEmptyEntries);
      if (split.Length != 2)
        return null;

      if (!int.TryParse(split[0], out var projectId))
        return null;

      return projectId;
    }
  };

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public async Task TestSimpleRecordCreateDeleteAsync(int _)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));
      var doc = await storage.WriteSimpleDocumentAsync(_entryId: 123, _data: "test_data", lifetime.Token);

      var data0 = await storage.ReadSimpleDocumentAsync<string>(_entryId: 123, lifetime.Token);

      Assert.Equal("test_data", data0?.Data);

      await storage.DeleteSimpleDocumentAsync<string>(123, lifetime.Token);
      var data1 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);

      Assert.Null(data1);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
#pragma warning disable IDE0060 // Remove unused parameter
  [Theory]
  [Repeat(100)]
  public async Task TestDocVersionLastModifiedAsync(int __)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));
      var doc0 = await storage.WriteSimpleDocumentAsync(123, "test_data", lifetime.Token);
      var doc1 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);

      Assert.Equal(doc0.Version, doc1?.Version);
      Assert.Equal(doc0.LastModified, doc1?.LastModified);
      Assert.Equal(doc0.Created, doc1?.Created);

      _ = await storage.WriteSimpleDocumentAsync(123, "test-data-new", lifetime.Token);

      var doc2 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);
      Assert.NotEqual(doc0.Version, doc2?.Version);
      Assert.NotEqual(doc0.LastModified, doc2?.LastModified);
      Assert.Equal(doc0.Created, doc2?.Created);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public async Task TestSimpleRecordUniqueAsync(int _)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));

      var record0 = await storage.WriteSimpleDocumentAsync(123, "test-data-0", lifetime.Token);
      var record1 = await storage.WriteSimpleDocumentAsync(123, "test-data-1", lifetime.Token);
      var record2 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);

      Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data);
      Assert.Equal("test-data-1", record2?.Data);
      Assert.Equal(record0.DocId, record2?.DocId);

      var list = await storage.ListSimpleDocumentsAsync<string>(_ct: lifetime.Token).ToListAsync(lifetime.Token);
      Assert.Single(list);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public async Task TestRecordUniqueAsync(int _)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var ns = "test_table";
      var key = "test-key";

      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));

      var record0 = await storage.WriteDocumentAsync(_namespace: ns, _key: key, _data: "test-data-0", lifetime.Token);

      var record1 = await storage.WriteDocumentAsync(ns, key, "test-data-1", lifetime.Token);

      var record2 = await storage.ReadDocumentAsync(ns, key, lifetime.Token);

      Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data.ToObject<string>());
      Assert.Equal("test-data-1", record2?.Data.ToObject<string>());
      Assert.Equal(record0.DocId, record2?.DocId);

      var list = await storage.ListDocumentsAsync(ns, _ct: lifetime.Token).ToListAsync(lifetime.Token);
      Assert.Single(list);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public async Task TestUniquenessOfRecordsAndDocsAsync(int _)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));

      var record0 = await storage.WriteDocumentAsync("test-table", "test-key", "test-data-0", lifetime.Token);
      Assert.Equal(0, record0.DocId);

      await storage.DeleteDocumentsAsync("test-table", "test-key", null, null, lifetime.Token);

      var list0 = await storage.ListDocumentsAsync("test-table", _ct: lifetime.Token).ToListAsync(lifetime.Token);
      Assert.Empty(list0);

      var record1 = await storage.WriteDocumentAsync("test-table", "test-key", "test-data-0", lifetime.Token);
      Assert.NotEqual(0, record1.DocId);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public async Task CheckIfDocIdCalculatedOnDbOpenAsync(int _)
  {
    var lifetime0 = new Lifetime();
    var lifetime1 = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      // open db, write documents, then close db
      var entriesCount = 100;
      var storage0 = lifetime0.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));
      var enumerable = Enumerable.Range(0, entriesCount);

      var lastDocId = 0;
      await Parallel.ForEachAsync(enumerable, lifetime0.Token, async (_key, _ct) =>
      {
        var document0 = await storage0.WriteDocumentAsync("test-table0", _key, "test-data", lifetime0.Token);
        var document1 = await storage0.WriteDocumentAsync("test-table1", _key, "test-data", lifetime0.Token);
        var document2 = await storage0.WriteDocumentAsync("test-table2", _key, "test-data", lifetime0.Token);

        lastDocId = Math.Max(lastDocId, document0.DocId);
        lastDocId = Math.Max(lastDocId, document1.DocId);
        lastDocId = Math.Max(lastDocId, document2.DocId);
      });

      await lifetime0.EndAsync();

      Assert.Equal(entriesCount * 3, lastDocId + 1);

      var storage1 = lifetime1.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));
      var document = await storage1.WriteDocumentAsync("test-table", entriesCount + 1, "test-data", lifetime1.Token);

      Assert.True(document.DocId > lastDocId);
    }
    finally
    {
      await lifetime0.EndAsync();
      await lifetime1.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task CheckAttributeAsync()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));

      var document0 = await storage.WriteSimpleDocumentAsync(100, new DataRecord(100, "100"), lifetime.Token);
      var document1 = await storage.ReadSimpleDocumentAsync<DataRecord>(100, lifetime.Token);
      var document2 = await storage.ReadDocumentAsync("simple-record", 100, lifetime.Token);

      Assert.NotNull(document0);
      Assert.NotNull(document1);
      Assert.NotNull(document2);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task CheckCountMethodAsync()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var wrongNs = "wrong_ns";
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));
      Assert.Equal(0, await storage.CountSimpleDocuments<DataRecord>(lifetime.Token));
      Assert.Equal(0, await storage.Count(wrongNs, lifetime.Token));

      for (int i = 0; i < 3; i++)
      {
        await storage.WriteSimpleDocumentAsync(i, new DataRecord(i, i.ToString()), lifetime.Token);
        Assert.Equal(i + 1, await storage.CountSimpleDocuments<DataRecord>(lifetime.Token));
        Assert.Equal(0, await storage.Count(wrongNs, lifetime.Token));
      }

      for (int i = 0; i < 3; i++)
      {
        await storage.DeleteSimpleDocumentAsync<DataRecord>(i, lifetime.Token);
        Assert.Equal(2 - i, await storage.CountSimpleDocuments<DataRecord>(lifetime.Token));
        Assert.Equal(0, await storage.Count(wrongNs, lifetime.Token));
      }
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task TestLikeOperatorSpeedAsync()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var totalEntriesCount = 10000;
      IDocumentStorage storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));
      for (var i = 0; i < 10000; i++)
      {
        var entry = new DataRecord(i % 100, string.Empty);
        _ = await storage.WriteSimpleDocumentAsync(entry.GetStorageKey(), entry, lifetime.Token);
      }

      var targetIdStart = 1;
      var targetIdMiddle = totalEntriesCount / 2;
      var targetIdEnd = totalEntriesCount - 1;

      // ==========================================================

      var targetIdSwStart = Stopwatch.StartNew();
      await foreach (var doc in storage.ListDocumentsMetaAsync("simple-record", _ct: lifetime.Token))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdStart)
          break;
      }
      targetIdSwStart.Stop();

      var targetIdSwStartLike = Stopwatch.StartNew();
      await foreach (var doc in storage.ListDocumentsMetaAsync("simple-record", _keyLikeExpression: new Data.LikeExpr($"{targetIdStart}.%"), _ct: lifetime.Token))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdStart)
          break;
      }
      targetIdSwStartLike.Stop();

      Assert.True(targetIdSwStartLike.Elapsed < targetIdSwStart.Elapsed);

      // ==========================================================

      var targetIdSwMiddle = Stopwatch.StartNew();
      await foreach (var doc in storage.ListDocumentsMetaAsync("simple-record", _ct: lifetime.Token))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdMiddle)
          break;
      }
      targetIdSwMiddle.Stop();

      var targetIdSwMiddleLike = Stopwatch.StartNew();
      await foreach (var doc in storage.ListDocumentsMetaAsync("simple-record", _keyLikeExpression: new Data.LikeExpr($"{targetIdMiddle}.%"), _ct: lifetime.Token))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdMiddle)
          break;
      }
      targetIdSwMiddleLike.Stop();

      Assert.True(targetIdSwMiddleLike.Elapsed < targetIdSwMiddle.Elapsed);

      // ==========================================================

      var targetIdSwEnd = Stopwatch.StartNew();
      await foreach (var doc in storage.ListDocumentsMetaAsync("simple-record", _ct: lifetime.Token))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdEnd)
          break;
      }
      targetIdSwEnd.Stop();

      var targetIdSwEndLike = Stopwatch.StartNew();
      await foreach (var doc in storage.ListDocumentsMetaAsync("simple-record", _keyLikeExpression: new Data.LikeExpr($"{targetIdEnd}.%"), _ct: lifetime.Token))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdEnd)
          break;
      }
      targetIdSwEndLike.Stop();

      Assert.True(targetIdSwEndLike.Elapsed < targetIdSwEnd.Elapsed);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task TestFlushFeatureAsync()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile));
      for (var i = 0; i < 1000; i++)
      {
        var entry = new DataRecord(i % 100, string.Empty);
        _ = await storage.WriteSimpleDocumentAsync(entry.GetStorageKey(), entry, lifetime.Token);
      }

      var walFile = new FileInfo($"{dbFile}-wal");
      Assert.True(walFile.Exists);

      var origWalFileSize = walFile.Length;
      await storage.FlushAsync(false, lifetime.Token);
      walFile.Refresh();
      Assert.Equal(origWalFileSize, walFile.Length);

      await storage.FlushAsync(true, lifetime.Token);
      walFile.Refresh();
      Assert.Equal(0, walFile.Length);
    }
    finally
    {
      await lifetime.EndAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";
}