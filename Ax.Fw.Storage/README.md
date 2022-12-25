# Ax.Fw.Storage
### Simple document storage provider based on SQLite
---
#### Why?
I know there are many no-sql embedded database engines. Why I selected SQLite? THB just for fun.
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
	
	// create document; pair 'namespace - key' is unique; any json serializable data can be stored
	var doc = await storage.WriteDocumentAsync(_namespace: "default", _key: "test-key", _data: "test-data-0", lifetime.Token);

	// retrieve data
	var readDoc = await storage.ReadDocumentAsync(_namespace: "default", _key: "test-key", lifetime.Token);

	Assert.Equal("test-data-0", readDoc?.Data.ToObject<string>());

	// there are also 'simple' documents; namespace of simple documents is automatically determined by data type
	var simpleDoc = await storage.WriteSimpleDocumentAsync(_entryId: 123, _data: "test_data", lifetime.Token);

	var readSimpleDoc = await storage.ReadSimpleDocumentAsync<string>(_entryId: 123, lifetime.Token);

	Assert.Equal("test_data", readSimpleDoc?.Data);
}
finally
{
	await lifetime.CompleteAsync();
	new FileInfo(dbFile).TryDelete();
}
```