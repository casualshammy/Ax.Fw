using Ax.Fw.Extensions;
using Ax.Fw.Storage.Attributes;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Toolkit;
using Ax.Fw.Tests.Tools;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Ax.Fw.Storage.Tests;

public class CachedSqliteDocumentStorageTests
{
  [SimpleDocument("simple-record")]
  record DataRecord(int Id, string Name);

  private readonly ITestOutputHelper p_output;

  public CachedSqliteDocumentStorageTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact]
  public async Task CompareCacheedAndNonCachedAsync()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var entries = Enumerable.Range(0, 1000).ToArray();
      var storage = new SqliteDocumentStorage(dbFile, lifetime);

      // warm-up
      foreach (var entry in entries)
      {
        await storage.WriteSimpleDocumentAsync(entry, new DataRecord(entry, entry.ToString()), lifetime.Token);
        var doc = await storage.ReadSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
        await storage.DeleteSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
      }

      // non-cached
      var sw = Stopwatch.StartNew();
      foreach (var entry in entries)
      {
        await storage.WriteSimpleDocumentAsync(entry, new DataRecord(entry, entry.ToString()), lifetime.Token);

        DocumentTypedEntry<DataRecord>? document = null;
        for (int i = 0; i < 100; i++)
          document = await storage.ReadSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
        Assert.NotNull(document);
        Assert.Equal(entry, document.Data.Id);

        await storage.DeleteSimpleDocumentAsync<DataRecord>(entry, lifetime.Token);
      }
      var elapsed = sw.Elapsed;
      p_output.WriteLine($"Non-cached: {elapsed}");

      // cached
      var cachedStorage = storage.ToCached(entries.Length, TimeSpan.FromSeconds(60));
      sw.Restart();
      foreach (var entry in entries)
      {
        await cachedStorage.WriteSimpleDocumentAsync(entry, new DataRecord(entry, entry.ToString()), lifetime.Token);

        DocumentTypedEntry<DataRecord>? document = null;
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
      await lifetime.CompleteAsync();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";

}