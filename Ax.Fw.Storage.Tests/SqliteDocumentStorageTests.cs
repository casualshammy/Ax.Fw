using Ax.Fw.Extensions;

namespace Ax.Fw.Storage.Tests;

public class SqliteDocumentStorageTests
{
    [Fact]
    public async Task TestDocumentCreateDeleteAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);

            var doc0 = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);
            var list0 = await storage.ListDocumentsAsync("test_doc_type", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list0);

            var doc1 = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);
            var list1 = await storage.ListDocumentsAsync("test_doc_type", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(2, list1.Count);

            var doc2 = await storage.CreateDocumentAsync("test_doc_type_hen", null, lifetime.Token);
            var list2 = await storage.ListDocumentsAsync("test_doc_type", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            var list2_0 = await storage.ListDocumentsAsync("test_doc_type_hen", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(2, list2.Count);
            Assert.Single(list2_0);

            await storage.DeleteDocumentAsync(doc0.DocId, lifetime.Token);
            var list3 = await storage.ListDocumentsAsync("test_doc_type", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list3);

            await storage.DeleteDocumentAsync(doc1.DocId, lifetime.Token);
            var list4 = await storage.ListDocumentsAsync("test_doc_type", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Empty(list4);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestSimpleRecordCreateDeleteAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
            var doc = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);

            var record = await storage.WriteSimpleRecordAsync(doc.DocId, "test-data", lifetime.Token);

            var data0 = await storage.ReadSimpleRecordAsync<string>(doc.DocId, lifetime.Token);

            Assert.Equal("test-data", data0?.Data);

            await storage.DeleteSimpleRecordAsync<string>(doc.DocId, lifetime.Token);
            var data1 = await storage.ReadSimpleRecordAsync<string>(doc.DocId, lifetime.Token);

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
            var doc0 = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);
            var doc1 = await storage.GetDocumentAsync(doc0.DocId, lifetime.Token);

            Assert.Equal(doc0.Version, doc1?.Version);
            Assert.Equal(doc0.LastModified, doc1?.LastModified);

            _ = await storage.WriteSimpleRecordAsync(doc0.DocId, "test-data", lifetime.Token);

            var doc2 = await storage.GetDocumentAsync(doc0.DocId, lifetime.Token);
            Assert.NotEqual(doc0.Version, doc2?.Version);
            Assert.NotEqual(doc0.LastModified, doc2?.LastModified);

            await storage.DeleteSimpleRecordAsync<string>(doc0.DocId, lifetime.Token);

            var doc3 = await storage.GetDocumentAsync(doc0.DocId, lifetime.Token);
            Assert.NotEqual(doc0.Version, doc3?.Version);
            Assert.NotEqual(doc0.LastModified, doc3?.LastModified);
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
            var doc = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);

            var record0 = await storage.WriteSimpleRecordAsync(doc.DocId, "test-data-0", lifetime.Token);

            var record1 = await storage.WriteSimpleRecordAsync(doc.DocId, "test-data-1", lifetime.Token);

            var record2 = await storage.ReadSimpleRecordAsync<string>(doc.DocId, lifetime.Token);

            Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data);
            Assert.Equal("test-data-1", record2?.Data);
            Assert.Equal(record0.RecordId, record2?.RecordId);

            var list = await storage.ListRecordsAsync(doc.DocId, null, null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
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
            var doc = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);

            var record0 = await storage.WriteRecordAsync(doc.DocId, "test-table", "test-key", "test-data-0", lifetime.Token);

            var record1 = await storage.WriteRecordAsync(doc.DocId, "test-table", "test-key", "test-data-1", lifetime.Token);

            var record2 = await storage.ReadRecordAsync(record1.DocId, lifetime.Token);

            Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data.ToObject<string>());
            Assert.Equal("test-data-1", record2?.Data.ToObject<string>());
            Assert.Equal(record0.RecordId, record2?.RecordId);

            var list = await storage.ListRecordsAsync(doc.DocId, null, null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestRecordWithNullKeyUniqueAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
            var doc = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);

            var record0 = await storage.WriteRecordAsync(doc.DocId, "test-table", null, "test-data-0", lifetime.Token);

            var record1 = await storage.WriteRecordAsync(doc.DocId, "test-table", null, "test-data-1", lifetime.Token);

            var record2 = await storage.ReadRecordAsync(record1.DocId, lifetime.Token);

            Assert.NotEqual(record0.Data.ToObject<string>(), record2?.Data.ToObject<string>());
            Assert.Equal("test-data-1", record2?.Data.ToObject<string>());
            Assert.Equal(record0.RecordId, record2?.RecordId);

            var list = await storage.ListRecordsAsync(doc.DocId, null, null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestRecordWithNullAndNotNullKeyUniqueAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
            var doc = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);

            var record0 = await storage.WriteRecordAsync(doc.DocId, "test-table", "test-key", "test-data-0", lifetime.Token);
            var record1 = await storage.WriteRecordAsync(doc.DocId, "test-table", null, "test-data-1", lifetime.Token);

            var record2 = await storage.ReadRecordAsync(record0.RecordId, lifetime.Token);
            var record3 = await storage.ReadRecordAsync(record1.RecordId, lifetime.Token);

            Assert.NotEqual(record0.RecordId, record1.RecordId);
            Assert.NotNull(record2?.RecordId);
            Assert.NotNull(record3?.RecordId);
            Assert.NotEqual(record2.RecordId, record3.RecordId);

            Assert.Equal("test-data-0", record2?.Data.ToObject<string>());
            Assert.Equal("test-data-1", record3?.Data.ToObject<string>());

            var list = await storage.ListRecordsAsync(doc.DocId, null, null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(2, list.Count);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    [Fact]
    public async Task TestDeleteRecordWithNullAndNotNullKeyUniqueAsync()
    {
        var lifetime = new Lifetime();
        var dbFile = GetDbTmpPath();
        try
        {
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
            var doc = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);

            var record0 = await storage.WriteRecordAsync(doc.DocId, "test-table", "test-key", "test-data-0", lifetime.Token);
            var record1 = await storage.WriteRecordAsync(doc.DocId, "test-table", null, "test-data-1", lifetime.Token);

            Assert.NotEqual(record0.RecordId, record1.RecordId);

            var list0 = await storage.ListRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(2, list0.Count);

            var list1 = await storage.ListRecordsAsync(doc.DocId, "test-table", "test-key", null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list1);

            await storage.DeleteRecordsAsync(doc.DocId, "test-table-invalid", null, null, null, lifetime.Token);
            var list2 = await storage.ListRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(2, list2.Count);

            await storage.DeleteRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token);
            var list3 = await storage.ListRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Empty(list3);

            var record2 = await storage.WriteRecordAsync(doc.DocId, "test-table", "test-key", "test-data-0", lifetime.Token);
            var record3 = await storage.WriteRecordAsync(doc.DocId, "test-table", null, "test-data-1", lifetime.Token);

            var list4 = await storage.ListRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Equal(2, list4.Count);

            await storage.DeleteRecordsAsync(doc.DocId, "test-table", "test-key", null, null, lifetime.Token);
            var list5 = await storage.ListRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Single(list5);

            await storage.DeleteRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token);
            var list6 = await storage.ListRecordsAsync(doc.DocId, "test-table", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Empty(list6);
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
            // docs
            var storage = new SqliteDocumentStorage(dbFile, lifetime);
            var doc0 = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);
            Assert.Equal(1, doc0.DocId);

            await storage.DeleteDocumentAsync(doc0.DocId, lifetime.Token);

            var list0 = await storage.ListDocumentsAsync("test_doc_type", null, null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Empty(list0);

            var doc1 = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);
            Assert.NotEqual(1, doc1.DocId);

            // records

            var record0 = await storage.WriteRecordAsync(doc1.DocId, "test-table", "test-key", "test-data-0", lifetime.Token);
            Assert.Equal(1, record0.RecordId);

            await storage.DeleteRecordsAsync(doc1.DocId, "test-table", "test-key", null, null, lifetime.Token);

            var list1 = await storage.ListRecordsAsync(doc1.DocId, "test-table", "test-key", null, null, lifetime.Token).ToListAsync(lifetime.Token);
            Assert.Empty(list1);

            var record1 = await storage.WriteRecordAsync(doc1.DocId, "test-table", "test-key", "test-data-0", lifetime.Token);
            Assert.NotEqual(1, record1.RecordId);
        }
        finally
        {
            await lifetime.CompleteAsync();
            new FileInfo(dbFile).TryDelete();
        }
    }

    private static string GetDbTmpPath() => $"{Path.GetTempFileName()}.sqlite";

}