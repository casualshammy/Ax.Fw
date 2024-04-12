using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Ax.Fw.Storage.Tests.Data;
using Ax.Fw.Tests.Tools;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Ax.Fw.Storage.Tests;

public class SqliteDocumentStorageAotTests
{
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public void TestSimpleRecordCreateDelete(int _)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));
      var doc = storage.WriteSimpleDocument(_entryId: 123, _data: "test_data", SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      var data0 = storage.ReadSimpleDocument<string>(_entryId: 123, SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      Assert.Equal("test_data", data0?.Data);

      storage.DeleteSimpleDocument<string>(123);
      var data1 = storage.ReadSimpleDocument<string>(123, SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      Assert.Null(data1);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
#pragma warning disable IDE0060 // Remove unused parameter
  [Theory]
  [Repeat(100)]
  public void TestDocVersionLastModified(int __)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));
      var doc0 = storage.WriteSimpleDocument(123, "test_data", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      var doc1 = storage.ReadSimpleDocument(123, SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      Assert.Equal(doc0.Version, doc1?.Version);
      Assert.Equal(doc0.LastModified, doc1?.LastModified);
      Assert.Equal(doc0.Created, doc1?.Created);

      _ = storage.WriteSimpleDocument(123, "test-data-new", SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      var doc2 = storage.ReadSimpleDocument(123, SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      Assert.NotEqual(doc0.Version, doc2?.Version);
      Assert.NotEqual(doc0.LastModified, doc2?.LastModified);
      Assert.Equal(doc0.Created, doc2?.Created);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public void TestSimpleRecordUnique(int _)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));

      var record0 = storage.WriteSimpleDocument(123, "test-data-0", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      var record1 = storage.WriteSimpleDocument(123, "test-data-1", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      var record2 = storage.ReadSimpleDocument<string>(123, SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      Assert.NotEqual(record0.Data, record2?.Data);
      Assert.Equal("test-data-1", record2?.Data);
      Assert.Equal(record0.DocId, record2?.DocId);

      var list = storage.ListSimpleDocuments(SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      Assert.Single(list);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public void TestRecordUnique(int _)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var ns = "test_table";
      var key = "test-key";

      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));

      var record0 = storage.WriteDocument(_namespace: ns, _key: key, _data: "test-data-0", SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      var record1 = storage.WriteDocument(ns, key, "test-data-1", SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      var record2 = storage.ReadDocument<string>(ns, key, SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      Assert.NotEqual(record0.Data, record2?.Data);
      Assert.Equal("test-data-1", record2?.Data);
      Assert.Equal(record0.DocId, record2?.DocId);

      var list = storage.ListDocuments<string>(ns, SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      Assert.Single(list);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public void TestUniquenessOfRecordsAndDocs(int _)
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));

      var record0 = storage.WriteDocument("test-table", "test-key", "test-data-0", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      Assert.Equal(0, record0.DocId);

      storage.DeleteDocuments("test-table", "test-key", null, null);

      var list0 = storage.ListDocuments<string>("test-table", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      Assert.Empty(list0);

      var record1 = storage.WriteDocument("test-table", "test-key", "test-data-0", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
      Assert.NotEqual(0, record1.DocId);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
  [Theory]
  [Repeat(100)]
  public void CheckIfDocIdCalculatedOnDbOpen(int _)
  {
    var lifetime0 = new Lifetime();
    var lifetime1 = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      // open db, write documents, then close db
      var entriesCount = 100;
      var storage0 = lifetime0.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));
      var enumerable = Enumerable.Range(0, entriesCount);

      var lastDocId = 0;
      Parallel.ForEach(enumerable, _key =>
      {
        var document0 = storage0.WriteDocument("test-table0", _key, "test-data", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
        var document1 = storage0.WriteDocument("test-table1", _key, "test-data", SqliteDocumentStorageAotTestsJsonCtx.Default.String);
        var document2 = storage0.WriteDocument("test-table2", _key, "test-data", SqliteDocumentStorageAotTestsJsonCtx.Default.String);

        lastDocId = Math.Max(lastDocId, document0.DocId);
        lastDocId = Math.Max(lastDocId, document1.DocId);
        lastDocId = Math.Max(lastDocId, document2.DocId);
      });

      lifetime0.End();

      Assert.Equal(entriesCount * 3, lastDocId + 1);

      var storage1 = lifetime1.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));
      var document = storage1.WriteDocument("test-table", entriesCount + 1, "test-data", SqliteDocumentStorageAotTestsJsonCtx.Default.String);

      Assert.True(document.DocId > lastDocId);
    }
    finally
    {
      lifetime0.End();
      lifetime1.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public void CheckAttribute()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));

      var document0 = storage.WriteSimpleDocument(100, new DataRecord(100, "100"), SqliteDocumentStorageAotTestsJsonCtx.Default.DataRecord);
      var document1 = storage.ReadSimpleDocument(100, SqliteDocumentStorageAotTestsJsonCtx.Default.DataRecord);
      var document2 = storage.ReadDocument("simple-record", 100, SqliteDocumentStorageAotTestsJsonCtx.Default.DataRecord);

      Assert.NotNull(document0);
      Assert.NotNull(document1);
      Assert.NotNull(document2);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public void CheckCountMethod()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var wrongNs = "wrong_ns";
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));
      Assert.Equal(0, storage.CountSimpleDocuments<DataRecord>(null));
      Assert.Equal(0, storage.Count(wrongNs, null));

      for (int i = 0; i < 3; i++)
      {
        storage.WriteSimpleDocument(i, new DataRecord(i, i.ToString()), SqliteDocumentStorageAotTestsJsonCtx.Default.DataRecord);
        Assert.Equal(i + 1, storage.CountSimpleDocuments<DataRecord>(null));
        Assert.Equal(0, storage.Count(wrongNs, null));
      }

      for (int i = 0; i < 3; i++)
      {
        storage.DeleteSimpleDocument<DataRecord>(i);
        Assert.Equal(2 - i, storage.CountSimpleDocuments<DataRecord>(null));
        Assert.Equal(0, storage.Count(wrongNs, null));
      }
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public void TestLikeOperatorSpeed()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var totalEntriesCount = 10000;
      IDocumentStorage storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));
      for (var i = 0; i < 10000; i++)
      {
        var entry = new DataRecord(i % 100, string.Empty);
        _ = storage.WriteSimpleDocument(entry.GetStorageKey(), entry, SqliteDocumentStorageAotTestsJsonCtx.Default.DataRecord);
      }

      var targetIdMiddle = totalEntriesCount / 2;
      var targetIdEnd = totalEntriesCount - 1;

      // ==========================================================

      var targetIdSwMiddle = Stopwatch.StartNew();
      foreach (var doc in storage.ListDocumentsMeta("simple-record"))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdMiddle)
          break;
      }
      targetIdSwMiddle.Stop();

      var targetIdSwMiddleLike = Stopwatch.StartNew();
      foreach (var doc in storage.ListDocumentsMeta("simple-record", _keyLikeExpression: new LikeExpr($"{targetIdMiddle}.%")))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdMiddle)
          break;
      }
      targetIdSwMiddleLike.Stop();

      Assert.True(targetIdSwMiddleLike.Elapsed < targetIdSwMiddle.Elapsed);

      // ==========================================================

      var targetIdSwEnd = Stopwatch.StartNew();
      foreach (var doc in storage.ListDocumentsMeta("simple-record"))
      {
        var id = DataRecord.GetIdFromStorageKey(doc.Key);
        if (id == targetIdEnd)
          break;
      }
      targetIdSwEnd.Stop();

      var targetIdSwEndLike = Stopwatch.StartNew();
      foreach (var doc in storage.ListDocumentsMeta("simple-record", _keyLikeExpression: new LikeExpr($"{targetIdEnd}.%")))
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
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public void TestFlushFeature()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));
      for (var i = 0; i < 1000; i++)
      {
        var entry = new DataRecord(i % 100, string.Empty);
        _ = storage.WriteSimpleDocument(entry.GetStorageKey(), entry, SqliteDocumentStorageAotTestsJsonCtx.Default.DataRecord);
      }

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
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public void InterfacesSerialization()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(dbFile, null));

      var list = new[] { 1, 2, 3 };
      var dictionary = ImmutableDictionary.CreateBuilder<string, int>();
      dictionary.Add("A", 1);
      dictionary.Add("b", 2);
      dictionary.Add("C", 3);
      var data = new InterfacesRecord(list, dictionary.ToImmutable(), RecordEnum.Record);

      storage.WriteSimpleDocument(0, data, SqliteDocumentStorageAotTestsJsonCtx.Default.InterfacesRecord);
      var result = storage.ReadSimpleDocument(0, SqliteDocumentStorageAotTestsJsonCtx.Default.InterfacesRecord);
      Assert.Equal(list, result?.Data.ListOfInt);
      Assert.Equal(dictionary, result?.Data.Dictionary);
      Assert.Equal(RecordEnum.Record, result?.Data.Enum);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";

}

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(DataRecord))]
[JsonSerializable(typeof(InterfacesRecord))]
internal partial class SqliteDocumentStorageAotTestsJsonCtx : JsonSerializerContext { }

