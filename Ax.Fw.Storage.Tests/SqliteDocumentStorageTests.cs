using Ax.Fw.Extensions;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Ax.Fw.Storage.Tests;

public class SqliteDocumentStorageTests
{
    private readonly ITestOutputHelper p_output;

    public SqliteDocumentStorageTests(ITestOutputHelper _output)
    {
        p_output = _output;
    }

    [Fact]
    public async Task TestSimpleRecordCreateDeleteAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
            var doc = await storage.WriteSimpleDocumentAsync(123, "test_data", lifetime.Token);

            var data0 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);

            Assert.Equal("test_data", data0?.Data);

            await storage.DeleteSimpleDocumentAsync<string>(123, lifetime.Token);
            var data1 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);

            Assert.Null(data1);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestDocVersionLastModifiedAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
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
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestSimpleRecordUniqueAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);

            var record0 = await storage.WriteSimpleDocumentAsync(123, "test-data-0", lifetime.Token);
            var record1 = await storage.WriteSimpleDocumentAsync(123, "test-data-1", lifetime.Token);
            var record2 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);

            Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data);
            Assert.Equal("test-data-1", record2?.Data);
            Assert.Equal(record0.DocId, record2?.DocId);

            var list = await storage.ListSimpleDocumentsAsync<string>(null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestRecordUniqueAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);

            var record0 = await storage.WriteDocumentAsync("test_table", "test-key", "test-data-0", lifetime.Token);

            var record1 = await storage.WriteDocumentAsync("test-table", "test-key", "test-data-1", lifetime.Token);

            var record2 = await storage.ReadDocumentAsync("test-table", "test-key", lifetime.Token);

            Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data.ToObject<string>());
            Assert.Equal("test-data-1", record2?.Data.ToObject<string>());
            Assert.Equal(record0.DocId, record2?.DocId);

            var list = await storage.ListDocumentsAsync("test-table", null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestUniquenessOfRecordsAndDocsAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);

            var record0 = await storage.WriteDocumentAsync("test-table", "test-key", "test-data-0", lifetime.Token);
            Assert.Equal(1, record0.DocId);

            await storage.DeleteDocumentsAsync("test-table", "test-key", null, null, lifetime.Token);

            var list0 = await storage.ListDocumentsAsync("test-table", null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Empty(list0);

            var record1 = await storage.WriteDocumentAsync("test-table", "test-key", "test-data-0", lifetime.Token);
            Assert.NotEqual(1, record1.DocId);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task StressTestWriteAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var entriesCount = 100000;
            var storage = new SqliteDocumentStorage(dbFile, lifetime);

            var enumerable = Enumerable.Range(0, entriesCount);

            var sw = Stopwatch.StartNew();

            await Parallel.ForEachAsync(enumerable, lifetime.Token, async (_key, _ct) =>
            {
                await storage.WriteDocumentAsync("test-table", _key, "test-data", lifetime.Token);
            });

            var writeElapsed = sw.Elapsed;

            var list = await storage.ListDocumentsAsync("test-table", null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(entriesCount, list.Count);

            var listElapsed = sw.Elapsed - writeElapsed;

            await Parallel.ForEachAsync(enumerable, lifetime.Token, async (_key, _ct) =>
            {
                var result = await storage.ReadDocumentAsync("test-table", _key, lifetime.Token);
                if (result == null)
                    Assert.Fail($"Entry is null!");
            });

            var readElapsed = sw.Elapsed - listElapsed - writeElapsed;

            p_output.WriteLine($"Write: {writeElapsed} ({writeElapsed.TotalMilliseconds / entriesCount} ms/entry)");
            p_output.WriteLine($"List: {listElapsed} ({listElapsed.TotalMilliseconds / entriesCount} ms/entry)");
            p_output.WriteLine($"Read: {readElapsed} ({readElapsed.TotalMilliseconds / entriesCount} ms/entry)");
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    private static string GetDbTmpPath() => $"{Path.GetTempFileName()}.sqlite";

}