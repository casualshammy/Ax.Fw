# Ax.Fw.Storage
### Simple document storage provider based on SQLite
---
#### Usage example:
```csharp
// getting lifetime
var lifetime = new Lifetime();

// getting db filepath
var dbFile = GetDbPath();

try
{
	var storage = new SqliteDocumentStorage(dbFile, lifetime);
	
	// create document; document instance contains meta data - namespace, version, last modified datetime, etc
	var doc = await storage.CreateDocumentAsync("test_doc_type", null, lifetime.Token);

	// create record; record instance contains actual data; one document can contain multiple records
	var record = await storage.WriteSimpleRecordAsync(doc.DocId, "test-data", lifetime.Token);

	// retrieve data; simple records in document are distinguished by type (strongly-typed)
	var data = await storage.ReadSimpleRecordAsync<string>(doc.DocId, lifetime.Token);

	Assert.Equal("test-data", data?.Data);
}
finally
{
	await lifetime.CompleteAsync();
	new FileInfo(dbFile).TryDelete();
}
```