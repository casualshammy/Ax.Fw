using Ax.Fw.Extensions;
using Ax.Fw.Tests.Tools;
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

    [Theory]
    [Repeat(100)]
    public async Task TestSimpleRecordCreateDeleteAsync(int _)
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
            var doc = await storage.WriteSimpleDocumentAsync(_entryId: 123, _data: "test_data", lifetime.Token);

            var data0 = await storage.ReadSimpleDocumentAsync<string>(_entryId: 123, lifetime.Token);

            Assert.Equal("test_data", data0?.Data);

            await storage.DeleteSimpleDocumentAsync<string>(123, lifetime.Token);
            var data1 = await storage.ReadSimpleDocumentAsync<string>(123, lifetime.Token);

            Assert.Null(data1);
        }
        finally
        {
            await lifetime.CompleteAsync();
            if (!new FileInfo(dbFile).TryDelete())
                Assert.Fail($"Can't delete file '{dbFile}'");
        }
    }

    [Theory]
    [Repeat(100)]
    public async Task TestDocVersionLastModifiedAsync(int __)
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
            if (!new FileInfo(dbFile).TryDelete())
                Assert.Fail($"Can't delete file '{dbFile}'");
        }
    }

    [Theory]
    [Repeat(100)]
    public async Task TestSimpleRecordUniqueAsync(int _)
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
            if (!new FileInfo(dbFile).TryDelete())
                Assert.Fail($"Can't delete file '{dbFile}'");
        }
    }

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

            var storage = new SqliteDocumentStorage(dbFile, lifetime);

            var record0 = await storage.WriteDocumentAsync(_namespace: ns, _key: key, _data: "test-data-0", lifetime.Token);

            var record1 = await storage.WriteDocumentAsync(ns, key, "test-data-1", lifetime.Token);

            var record2 = await storage.ReadDocumentAsync(ns, key, lifetime.Token);

            Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data.ToObject<string>());
            Assert.Equal("test-data-1", record2?.Data.ToObject<string>());
            Assert.Equal(record0.DocId, record2?.DocId);

            var list = await storage.ListDocumentsAsync(ns, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list);
        }
        finally
        {
            await lifetime.CompleteAsync();
            if (!new FileInfo(dbFile).TryDelete())
                Assert.Fail($"Can't delete file '{dbFile}'");
        }
    }

    [Theory]
    [Repeat(100)]
    public async Task TestUniquenessOfRecordsAndDocsAsync(int _)
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
            if (!new FileInfo(dbFile).TryDelete())
                Assert.Fail($"Can't delete file '{dbFile}'");
        }
    }

    [Fact]
    public async Task StressTestAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var entriesCount = 10000;
            var storage = new SqliteDocumentStorage(dbFile, lifetime);

            var enumerable = Enumerable.Range(0, entriesCount);

            var sw = Stopwatch.StartNew();

            await Parallel.ForEachAsync(enumerable, lifetime.Token, async (_key, _ct) =>
            {
                var i = _key;
                await storage.WriteDocumentAsync("test-table", _key, $"test-data-{i}", lifetime.Token);
            });

            var writeElapsed = sw.Elapsed;

            var list = await storage.ListDocumentsAsync("test-table", null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(entriesCount, list.Count);

            var listElapsed = sw.Elapsed - writeElapsed;

            await Parallel.ForEachAsync(enumerable, lifetime.Token, async (_key, _ct) =>
            {
                var i = _key;

                var result = await storage.ReadTypedDocumentAsync<string>("test-table", _key, lifetime.Token);
                if (result == null)
                    Assert.Fail($"Entry is null!");

                if (result.Data != $"test-data-{i}")
                    Assert.Fail($"Entry is incorrect!");
            });

            var readElapsed = sw.Elapsed - listElapsed - writeElapsed;

            p_output.WriteLine($"Write: {writeElapsed} ({writeElapsed.TotalMilliseconds / entriesCount} ms/entry)");
            p_output.WriteLine($"List: {listElapsed} ({listElapsed.TotalMilliseconds / entriesCount} ms/entry)");
            p_output.WriteLine($"Read: {readElapsed} ({readElapsed.TotalMilliseconds / entriesCount} ms/entry)");
        }
        finally
        {
            await lifetime.CompleteAsync();
            if (!new FileInfo(dbFile).TryDelete())
                Assert.Fail($"Can't delete file '{dbFile}'");
        }
    }

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
            var storage0 = new SqliteDocumentStorage(dbFile, lifetime0);
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

            await lifetime0.CompleteAsync();

            Assert.Equal(entriesCount * 3, lastDocId);

            var storage1 = new SqliteDocumentStorage(dbFile, lifetime1);
            var document = await storage1.WriteDocumentAsync("test-table", entriesCount + 1, "test-data", lifetime1.Token);

            Assert.True(document.DocId > lastDocId);
        }
        finally
        {
            await lifetime0.CompleteAsync();
            await lifetime1.CompleteAsync();
            if (!new FileInfo(dbFile).TryDelete())
                Assert.Fail($"Can't delete file '{dbFile}'");
        }
    }

    private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";

}