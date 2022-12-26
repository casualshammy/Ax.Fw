# Ax.Fw.Storage
Simple document database; based on SQLite
## Why?
I just wanted to create an instance of class and write to it, read from it, list it's elements by key without thinking about "this database's BSON don't support ImmutableDictionary, meh", "that database doesn't support numeric keys, meh", "ah, I can't compare integer row with unsigned integer variable using that database, meh" and so on. Why not "key-value" storage? I also wanted to filter my objects by last changed datetime. That's it. My database is not fast as more "strongly-typed" databases (I think it's 1.1 - 1.5x slower in some scenarios), but it is reliable as any SQLite database.
## Usage example:
```csharp
// getting lifetime
var lifetime = new Lifetime();

// getting db filepath
var dbFile = GetDbPath();

try
{
	// create database or open existing; you can omit lifetime parameter, but you should call `Dispose` in this case
	var storage = new SqliteDocumentStorage(dbFile, lifetime);
	
	// create document; pair 'namespace - key' is unique; any json serializable data can be stored
	var doc = await storage.WriteDocumentAsync(_namespace: "default", _key: "test-key", _data: "test-data-0", lifetime.Token);

	// retrieve data
	var readDoc = await storage.ReadDocumentAsync(_namespace: "default", _key: "test-key", lifetime.Token);

	Assert.Equal("test-data-0", readDoc?.Data.ToObject<string>());

	// there are also 'simple' documents; 
	// namespace of simple documents is automatically determined by data type or by `SimpleDocumentAttribute`
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
