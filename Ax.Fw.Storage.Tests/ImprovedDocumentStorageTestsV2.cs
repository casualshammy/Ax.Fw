using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Attributes;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Tests.Data;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace Ax.Fw.Storage.Tests;

public class ImprovedDocumentStorageTestsV2
{
  private readonly ITestOutputHelper p_output;

  public ImprovedDocumentStorageTestsV2(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact]
  public async Task CompareCachedAndNonCachedAsync()
  {
    var lifetime = new Lifetime();
    try
    {
      var entries = Enumerable.Range(0, 1000).ToArray();
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorage(GetDbTmpPath(), ImprovedDocumentStorageTestsV2JsonCtx.Default));
      var cachedStorage = lifetime.ToDisposeOnEnding(
        new SqliteDocumentStorage(GetDbTmpPath(), ImprovedDocumentStorageTestsV2JsonCtx.Default, new StorageCacheOptions(entries.Length, TimeSpan.FromSeconds(60))));

      // warm-up
      foreach (var entry in entries)
      {
        await storage.WriteSimpleDocumentAsync(entry, new DataRecord(entry, entry.ToString()), lifetime.Token);
        _ = await storage.ReadSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
        await storage.DeleteSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);

        await cachedStorage.WriteSimpleDocumentAsync(entry, new DataRecord(entry, entry.ToString()), lifetime.Token);
        _ = await cachedStorage.ReadSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
        await cachedStorage.DeleteSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
      }

      // non-cached
      var sw = Stopwatch.StartNew();
      foreach (var entry in entries)
      {
        await storage.WriteSimpleDocumentAsync(entry, new DataRecord(entry, entry.ToString()), lifetime.Token);

        DocumentEntry<DataRecord>? document = null;
        for (int i = 0; i < 100; i++)
          document = await storage.ReadSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
        Assert.NotNull(document);
        Assert.Equal(entry, document.Data.Id);

        await storage.DeleteSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
      }
      var elapsed = sw.Elapsed;
      p_output.WriteLine($"Non-cached: {elapsed}");

      // cached
      sw.Restart();
      foreach (var entry in entries)
      {
        await cachedStorage.WriteSimpleDocumentAsync(entry, new DataRecord(entry, entry.ToString()), lifetime.Token);

        DocumentEntry<DataRecord>? document = null;
        for (int i = 0; i < 100; i++)
          document = await cachedStorage.ReadSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
        Assert.NotNull(document);
        Assert.Equal(entry, document.Data.Id);

        await cachedStorage.DeleteSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
      }
      var cachedElapsed = sw.Elapsed;
      p_output.WriteLine($"Cached: {cachedElapsed}");

      Assert.True(cachedElapsed < elapsed);
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public async Task CheckRetentionRulesAsync()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var entries = Enumerable.Range(0, 1000).ToArray();

      var counter = 0;
      var storage = new SqliteDocumentStorage(
        dbFile,
        ImprovedDocumentStorageTestsV2JsonCtx.Default,
        null,
        new StorageRetentionOptions(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), _deletedDocsMeta => counter += _deletedDocsMeta.Count));

      lifetime.ToDisposeOnEnding(storage);

      await storage.WriteSimpleDocumentAsync(100, new DataRecord(100, "entry-100"), lifetime.Token);
      var doc0 = await storage.ReadSimpleDocumentAsync<DataRecord>(100, lifetime.Token);
      Assert.NotNull(doc0);

      await Task.Delay(TimeSpan.FromSeconds(2), lifetime.Token);
      Assert.Equal(1, counter);
      var doc1 = await storage.ReadSimpleDocumentAsync<DataRecord>(100, lifetime.Token);
      Assert.Null(doc1);
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

[JsonSerializable(typeof(DataRecord))]
internal partial class ImprovedDocumentStorageTestsV2JsonCtx : JsonSerializerContext { }