# Ax.Fw.Storage
Simple document database; based on SQLite
## Why?
I just wanted to create an instance of class and be able to write to database, read from database, enumerate elements in database by key without thinking about "this database's BSON don't support ImmutableDictionary, meh", "that database doesn't support numeric keys, meh", "ah, I can't search for values of integer column using unsigned integer variable, meh" and so on. Why not "key-value" storage? I also wanted to filter my objects by `last changed` time. That's it. This document storage engine is not as fast as more "strongly-typed" databases (I think it's 1.1 - 1.5x slower in some scenarios), but it is reliable as any SQLite database.
## Usage example:
```csharp
// we need CancellationToken
var ct = default(CancellationToken);

// path to database file
var dbFile = "/home/user/data.db"

try
{
    // create database or open existing
    using var storage = new SqliteDocumentStorage(dbFile);

    // create document; pair 'namespace - key' is unique; any json serializable data can be stored
    var doc = await storage.WriteDocumentAsync(_namespace: "default", _key: "test-key", _data: "test-data-0", ct);

    // retrieve data
    var readDoc = await storage.ReadDocumentAsync(_namespace: "default", _key: "test-key", ct);

    Assert.Equal("test-data-0", readDoc?.Data.ToObject<string>());

    // there are also 'simple' documents; 
    // namespace of simple documents is automatically determined by data type or by `SimpleDocumentAttribute`
    var simpleDoc = await storage.WriteSimpleDocumentAsync(_entryId: 123, _data: "test_data", ct);

    var readSimpleDoc = await storage.ReadSimpleDocumentAsync<string>(_entryId: 123, ct);

    Assert.Equal("test_data", readSimpleDoc?.Data);

    // you also can attach in-memory cache to document storage
    // cache makes read operations significantly faster
    using var cachedStorage = storage.WithCache(_maxValuesCached: 1000, _cacheTtl: TimeSpan.FromSeconds(60));

    // you also can attach retention rules to document storage
    // documents older than certain age will be automatically deleted
    using var storageWithRules = storage
      .WithRetentionRules(TimeSpan.FromHours(1), TimeSpan.FromHours(1), TimeSpan.FromMinutes(10));
}
finally
{
    new FileInfo(dbFile).TryDelete();
}
```
