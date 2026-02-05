using Ax.Fw.Extensions;
using LiteDB;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace Ax.Fw.Storage.Tests;

public class StressTests
{
  record LiteDbEntry(int Key, string Value);

  private const int PROBLEM_SIZE = 1000;

  private readonly ITestOutputHelper p_output;

  public StressTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact]
  public void StressTestSqlite()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorageV2(dbFile, CompareDbTestsV2JsonCtx.Default));

      var enumerable = Enumerable.Range(0, PROBLEM_SIZE);

      var sw = Stopwatch.StartNew();

      Parallel.ForEach(enumerable, _key =>
      {
        var i = _key;
        storage.WriteDocument("test-table", _key, $"test-data-{i}");
      });
      var writeElapsed = sw.Elapsed;

      var list = storage.ListDocuments<string>("test-table");
      Assert.Equal(PROBLEM_SIZE, list.Count());
      var listElapsed = sw.Elapsed - writeElapsed;

      Parallel.ForEach(enumerable, _key =>
      {
        var i = _key;

        var result = storage.ReadDocument<string>("test-table", _key);
        if (result == null)
          Assert.Fail($"Entry is null!");

        if (result.Data != $"test-data-{i}")
          Assert.Fail($"Entry is incorrect!");
      });

      var readElapsed = sw.Elapsed - listElapsed - writeElapsed;

      p_output.WriteLine($"SQLite Write: {writeElapsed} ({writeElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      p_output.WriteLine($"SQLite List: {listElapsed} ({listElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      p_output.WriteLine($"SQLite Read: {readElapsed} ({readElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");

      Console.WriteLine($"SQLite Write: {writeElapsed} ({writeElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      Console.WriteLine($"SQLite List: {listElapsed} ({listElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      Console.WriteLine($"SQLite Read: {readElapsed} ({readElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public void SqliteList()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorageV2(dbFile, CompareDbTestsV2JsonCtx.Default));

      var enumerable = Enumerable.Range(0, PROBLEM_SIZE);

      Parallel.ForEach(enumerable, _key =>
      {
        var i = _key;
        storage.WriteDocument("test-table", _key, $"test-data-{i}");
      });

      var list = storage.ListDocuments<string>("test-table");
      Assert.Equal(PROBLEM_SIZE, list.Count());

      Parallel.ForEach(enumerable, _key =>
      {
        var list = storage.ListDocuments<string>("test-table");
        Assert.Equal(PROBLEM_SIZE, list.Count());
      });
    }
    finally
    {
      lifetime.End();
      //if (!new FileInfo(dbFile).TryDelete())
      //  Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  [Fact]
  public void StressTestLiteDbAsync()
  {
    var lifetime = new Lifetime();
    var dbFile = GetDbTmpPath();
    try
    {
      var storage = lifetime.ToDisposeOnEnding(new LiteDatabase(dbFile));
      var col = storage.GetCollection<LiteDbEntry>("default");

      var enumerable = Enumerable.Range(0, PROBLEM_SIZE);

      var sw = Stopwatch.StartNew();

      Parallel.ForEach(enumerable, _key =>
      {
        var i = _key;
        col.Insert(new LiteDbEntry(i, $"test-data-{i}"));
      });

      var writeElapsed = sw.Elapsed;

      var list = col.Query().ToArray();
      Assert.Equal(PROBLEM_SIZE, list.Length);

      var listElapsed = sw.Elapsed - writeElapsed;

      col.EnsureIndex(_x => _x.Key);

      Parallel.ForEach(enumerable, _key =>
      {
        var i = _key;

        var result = col.FindOne(_x => _x.Key == i);
        if (result == null)
          Assert.Fail($"Entry is null!");

        if (result.Value != $"test-data-{i}")
          Assert.Fail($"Entry is incorrect!");
      });

      var readElapsed = sw.Elapsed - listElapsed - writeElapsed;

      p_output.WriteLine($"LiteDB Write: {writeElapsed} ({writeElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      p_output.WriteLine($"LiteDB List: {listElapsed} ({listElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      p_output.WriteLine($"LiteDB Read: {readElapsed} ({readElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");

      Console.WriteLine($"LiteDB Write: {writeElapsed} ({writeElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      Console.WriteLine($"LiteDB List: {listElapsed} ({listElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
      Console.WriteLine($"LiteDB Read: {readElapsed} ({readElapsed.TotalMilliseconds / PROBLEM_SIZE} ms/entry)");
    }
    finally
    {
      lifetime.End();
      if (!new FileInfo(dbFile).TryDelete())
        Assert.Fail($"Can't delete file '{dbFile}'");
    }
  }

  private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";

}

[JsonSerializable(typeof(string))]
internal partial class CompareDbTestsV2JsonCtx : JsonSerializerContext { }