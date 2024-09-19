using Ax.Fw.Cache;
using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Microsoft.Data.Sqlite;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Storage;

public class SqliteDocumentStorage : DisposableStack, IDocumentStorage
{
  record CacheKey(string Namespace, string Key);

  private readonly string p_dbFilePath;
  private readonly JsonSerializerContext? p_jsonCtx;
  private readonly SemaphoreSlim p_writeSemaphore;
  private readonly SyncCache<CacheKey, object>? p_cache;
  private long p_documentsCounter = 0;

  /// <summary>
  /// Opens existing database or creates new
  /// </summary>
  /// <param name="_dbFilePath">Path to database file</param>
  /// <param name="_jsonCtx">Serialization context that will be used for internal (de-)serialization</param>
  public SqliteDocumentStorage(
    string _dbFilePath,
    JsonSerializerContext? _jsonCtx,
    StorageCacheOptions? _cacheOptions = null,
    StorageRetentionOptions? _retentionOptions = null)
  {
    p_dbFilePath = _dbFilePath;
    p_jsonCtx = _jsonCtx;
    p_writeSemaphore = ToDisposeOnEnded(new SemaphoreSlim(1, 1));

    ToDoOnEnded(() => SqliteConnection.ClearAllPools());

    using (var connection = GetConnection())
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText =
          $"PRAGMA synchronous = NORMAL; " +
          $"PRAGMA journal_mode = WAL; " +
          $"PRAGMA case_sensitive_like = true; " +
          $"CREATE TABLE IF NOT EXISTS document_data " +
          $"( " +
          $"  doc_id INTEGER PRIMARY KEY, " +
          $"  namespace TEXT NOT NULL, " +
          $"  key TEXT NOT NULL, " +
          $"  last_modified INTEGER NOT NULL, " +
          $"  created INTEGER NOT NULL, " +
          $"  version INTEGER NOT NULL, " +
          $"  data TEXT NOT NULL, " +
          $"  UNIQUE(namespace, key) " +
          $"); " +
          $"CREATE INDEX IF NOT EXISTS index_namespace_key ON document_data (namespace, key); " +
          $"CREATE INDEX IF NOT EXISTS index_key ON document_data (key); " +
          $"CREATE INDEX IF NOT EXISTS index_namespace ON document_data (namespace); ";

        command.ExecuteNonQuery();
      }

      var counter = -1L;
      using (var cmd = connection.CreateCommand())
      {
        cmd.CommandText =
          $"SELECT MAX(doc_id) " +
          $"FROM document_data; ";

        try
        {
          var max = (long?)cmd.ExecuteScalar() ?? 0;

          counter = Math.Max(counter, max);
        }
        catch { }
      }

      p_documentsCounter = counter;
    }

    if (_cacheOptions != null)
    {
      var cacheMaxValues = _cacheOptions.CacheCapacity - _cacheOptions.CacheCapacity / 10;
      var cacheOverhead = _cacheOptions.CacheCapacity / 10;
      p_cache = new SyncCache<CacheKey, object>(new SyncCacheSettings(cacheMaxValues, cacheOverhead, _cacheOptions.CacheTTL));
    }

    if (_retentionOptions != null)
    {
      var scheduler = ToDisposeOnEnded(new EventLoopScheduler());

      var subs = Observable
        .Interval(_retentionOptions.ScanInterval ?? TimeSpan.FromMinutes(10), scheduler)
        .StartWithDefault()
        .ObserveOn(scheduler)
        .Subscribe(_ =>
        {
          var now = DateTimeOffset.UtcNow;
          var docsToDelete = new HashSet<DocumentEntryMeta>();

          foreach (var rule in _retentionOptions.Rules)
          {
            foreach (var doc in ListDocumentsMeta(rule.Namespace))
            {
              var docAge = now - doc.Created;
              var docLastModifiedAge = now - doc.LastModified;
              if (rule.DocumentMaxAgeFromCreation != null && docAge > rule.DocumentMaxAgeFromCreation)
                docsToDelete.Add(doc);
              else if (rule.DocumentMaxAgeFromLastChange != null && docLastModifiedAge > rule.DocumentMaxAgeFromLastChange)
                docsToDelete.Add(doc);
            }
          }

          foreach (var doc in docsToDelete)
          {
            DeleteDocuments(doc.Namespace, doc.Key, null, null);
            RemoveEntriesFromCache(doc.Namespace, doc.Key);
          }

          if (docsToDelete.Count > 0 && _retentionOptions.OnDocsDeleteCallback != null)
          {
            try
            {
              _retentionOptions.OnDocsDeleteCallback.Invoke(docsToDelete);
            }
            catch { }
          }
        });

      ToDispose(subs);
    }
  }

  private DocumentEntry<T> WriteDocumentInternal<T>(
    string _namespace,
    string _key,
    T _data)
  {
    var now = DateTimeOffset.UtcNow;
    string json;
    if (p_jsonCtx != null)
      json = JsonSerializer.Serialize(_data, typeof(T), p_jsonCtx);
    else
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
      json = JsonSerializer.Serialize(_data);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

    const string insertSql =
      $"INSERT OR REPLACE INTO document_data (doc_id, namespace, key, last_modified, created, version, data) " +
      $"VALUES (@doc_id, @namespace, @key, @last_modified, @created, @version, @data) " +
      $"ON CONFLICT (namespace, key) " +
      $"DO UPDATE SET " +
      $"  last_modified=@last_modified, " +
      $"  version=version+1, " +
      $"  data=@data " +
      $"RETURNING doc_id, version, created; ";

    p_writeSemaphore.Wait();
    try
    {
      using var connection = GetConnection();
      using var command = connection.CreateCommand();
      command.CommandText = insertSql;
      command.Parameters.AddWithValue("@doc_id", Interlocked.Increment(ref p_documentsCounter));
      command.Parameters.AddWithValue("@namespace", _namespace);
      command.Parameters.AddWithValue("@key", _key);
      command.Parameters.AddWithValue("@last_modified", now.UtcTicks);
      command.Parameters.AddWithValue("@created", now.UtcTicks);
      command.Parameters.AddWithValue("@version", 1);
      command.Parameters.AddWithValue("@data", json);

      using var reader = command.ExecuteReader();
      if (reader.Read())
      {
        var docId = reader.GetInt32(0);
        var version = reader.GetInt64(1);
        var created = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
        return new DocumentEntry<T>(docId, _namespace, _key, now, created, version, _data);
      }

      throw new InvalidOperationException($"Can't create document - db reader returned no result");
    }
    finally
    {
      p_writeSemaphore.Release();
    }
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public DocumentEntry<T> WriteDocument<T>(
    string _namespace,
    KeyAlike _key,
    T _data) where T : notnull
  {
    var document = WriteDocumentInternal(_namespace, _key.Key, _data);
    p_cache?.Put(new CacheKey(_namespace, _key.Key), document);
    return document;
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public DocumentEntry<T> WriteSimpleDocument<T>(
    KeyAlike _key,
    T _data) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return WriteDocument(ns, _key, _data);
  }

  /// <summary>
  /// Delete document from the database
  /// </summary>
  public void DeleteDocuments(
    string _namespace,
    KeyAlike? _key,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    const string deleteSql =
      $"DELETE FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  (@key IS NULL OR @key=key) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    using var connection = GetConnection();

    try
    {
      using var cmd = connection.CreateCommand();
      cmd.CommandText = deleteSql;
      cmd.Parameters.AddWithValue("@namespace", _namespace);
      cmd.Parameters.AddWithNullableValue("@key", _key?.Key);
      cmd.Parameters.AddWithNullableValue("@from", _from?.UtcTicks);
      cmd.Parameters.AddWithNullableValue("@to", _to?.UtcTicks);

      cmd.ExecuteNonQuery();
    }
    finally
    {
      RemoveEntriesFromCache(_namespace, _key?.Key);
    }
  }

  /// <summary>
  /// Delete document from the database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public void DeleteSimpleDocument<T>(
    KeyAlike _key) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    DeleteDocuments(ns, _key, null, null);
  }

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  public IEnumerable<DocumentEntryMeta> ListDocumentsMeta(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    const string listSql =
      $"SELECT doc_id, namespace, key, last_modified, created, version " +
      $"FROM document_data " +
      $"WHERE " +
      $"  (@namespace IS NULL OR @namespace=namespace) AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    using var connection = GetConnection();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = listSql;

    cmd.Parameters.AddWithNullableValue("@namespace", _namespace);
    cmd.Parameters.AddWithNullableValue("@key_like", _keyLikeExpression?.Pattern);
    cmd.Parameters.AddWithNullableValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithNullableValue("@to", _to?.UtcTicks);

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
      var docId = reader.GetInt32(0);
      var optionalKey = reader.GetString(2);
      var lastModified = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
      var created = new DateTimeOffset(reader.GetInt64(4), TimeSpan.Zero);
      var version = reader.GetInt64(5);

      yield return new DocumentEntryMeta(docId, _namespace ?? reader.GetString(1), optionalKey, lastModified, created, version);
    }
  }

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  public IEnumerable<DocumentEntryMeta> ListDocumentsMeta(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    const string listSql =
      $"SELECT doc_id, namespace, key, last_modified, created, version " +
      $"FROM document_data " +
      $"WHERE " +
      $"  (@namespace_like IS NULL OR namespace LIKE @namespace_like) AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    using var connection = GetConnection();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = listSql;

    cmd.Parameters.AddWithNullableValue("@namespace_like", _namespaceLikeExpression?.Pattern);
    cmd.Parameters.AddWithNullableValue("@key_like", _keyLikeExpression?.Pattern);
    cmd.Parameters.AddWithNullableValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithNullableValue("@to", _to?.UtcTicks);

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
      var docId = reader.GetInt32(0);
      var ns = reader.GetString(1);
      var optionalKey = reader.GetString(2);
      var lastModified = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
      var created = new DateTimeOffset(reader.GetInt64(4), TimeSpan.Zero);
      var version = reader.GetInt64(5);

      yield return new DocumentEntryMeta(docId, ns, optionalKey, lastModified, created, version);
    }
  }

  /// <summary>
  /// List documents
  /// </summary>
  public IEnumerable<DocumentEntry<T>> ListDocuments<T>(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    const string listSql =
      $"SELECT doc_id, key, last_modified, created, version, data " +
      $"FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    using var connection = GetConnection();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = listSql;

    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithNullableValue("@key_like", _keyLikeExpression?.Pattern);
    cmd.Parameters.AddWithNullableValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithNullableValue("@to", _to?.UtcTicks);

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
      var docId = reader.GetInt32(0);
      var key = reader.GetString(1);
      var lastModified = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
      var created = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
      var version = reader.GetInt64(4);

      T? data;
      if (p_jsonCtx != null)
        data = (T?)JsonSerializer.Deserialize(reader.GetString(5), typeof(T), p_jsonCtx);
      else
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        data = JsonSerializer.Deserialize<T>(reader.GetString(5));
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

      if (data == null)
        throw new FormatException($"Data of document '{docId}' is malformed!");

      yield return new DocumentEntry<T>(docId, _namespace, key, lastModified, created, version, data);
    }
  }

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public IEnumerable<DocumentEntry<T>> ListSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ListDocuments<T>(ns, _keyLikeExpression, _from, _to);
  }

  private DocumentEntry<T>? ReadDocumentInternal<T>(
    string _namespace,
    string _key)
  {
    const string readSql =
      $"SELECT doc_id, key, last_modified, created, version, data " +
      $"FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  key=@key; ";

    using var connection = GetConnection();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = readSql;
    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithValue("@key", _key);

    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
      var docId = reader.GetInt32(0);
      var optionalKey = reader.GetString(1);
      var lastModified = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
      var created = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
      var version = reader.GetInt64(4);
      var json = reader.GetString(5);

      T? data;
      if (p_jsonCtx != null)
        data = (T?)JsonSerializer.Deserialize(json, typeof(T), p_jsonCtx);
      else
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        data = JsonSerializer.Deserialize<T>(json);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

      if (data == null)
        throw new FormatException($"Data of document '{docId}' is malformed!");

      return new DocumentEntry<T>(docId, _namespace, optionalKey, lastModified, created, version, data);
    }

    return null;
  }

  /// <summary>
  /// Read document from the database
  /// </summary>
  public DocumentEntry<T>? ReadDocument<T>(
    string _namespace,
    KeyAlike _key)
  {
    var key = _key.Key;
    if (p_cache?.TryGet(new CacheKey(_namespace, key), out var cachedValue) == true)
      return cachedValue as DocumentEntry<T>;

    var result = ReadDocumentInternal<T>(_namespace, key);

    p_cache?.Put(new CacheKey(_namespace, key), result);

    return result;
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public DocumentEntry<T>? ReadSimpleDocument<T>(
    KeyAlike _key) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ReadDocument<T>(ns, _key);
  }

  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  public void CompactDatabase()
  {
    using var connection = GetConnection();
    using var command = connection.CreateCommand();
    command.CommandText = "VACUUM;";
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Flushes temporary file to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  public void Flush(bool _force)
  {
    using var connection = GetConnection();
    using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA wal_checkpoint({(_force ? "TRUNCATE" : "PASSIVE")});";
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  public int Count(
    string? _namespace,
    LikeExpr? _keyLikeExpression)
  {
    const string readSql =
      $"SELECT COUNT(*) " +
      $"FROM document_data " +
      $"WHERE " +
      $"  (@namespace IS NULL OR @namespace=namespace) AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like); ";

    using var connection = GetConnection();

    using var cmd = connection.CreateCommand();
    cmd.CommandText = readSql;

    cmd.Parameters.AddWithNullableValue("@namespace", _namespace);
    cmd.Parameters.AddWithNullableValue("@key_like", _keyLikeExpression?.Pattern);

    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
      var count = reader.GetInt32(0);
      return count;
    }

    return 0;
  }

  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  public int CountSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return Count(ns, _keyLikeExpression);
  }

  private void RemoveEntriesFromCache(string _namespace, string? _key)
  {
    if (p_cache == null)
      return;

    if (_key != null)
    {
      var cacheKey = new CacheKey(_namespace, _key);
      p_cache.TryRemove(cacheKey, out _);
      return;
    }

    foreach (var cacheEntry in p_cache.GetValues())
    {
      var cacheKey = cacheEntry.Key;
      var cacheValue = cacheEntry.Value;
      if (cacheValue == null)
        continue;

      if (_namespace == cacheKey.Namespace)
        p_cache.TryRemove(cacheKey, out _);
    }
  }

  private SqliteConnection GetConnection()
  {
    var connection = new SqliteConnection($"Data Source={p_dbFilePath};");
    connection.Open();
    return connection;
  }

}
