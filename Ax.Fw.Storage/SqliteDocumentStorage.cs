using Ax.Fw.Cache;
using Ax.Fw.Extensions;
using Ax.Fw.Pools;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Microsoft.Data.Sqlite;
using System.Collections.Immutable;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Ax.Fw.Storage;

public class SqliteDocumentStorage : DisposableStack, IDocumentStorage
{
  record CacheKey(string Namespace, string Key);

  private readonly SemaphoreSlim p_accessSemaphore;
  private readonly SqliteConnection p_connection;
  private readonly JsonSerializerContext? p_jsonCtx;
  private readonly SyncCache<CacheKey, object>? p_cache;
  private long p_documentsCounter = 0;

  /// <summary>
  /// Opens existing database or creates new
  /// </summary>
  /// <param name="_dbFilePath">Path to database file</param>
  /// <param name="_jsonCtx">Serialization context that will be used with methods without <see cref="JsonTypeInfo"/> parameter</param>
  public SqliteDocumentStorage(
    string _dbFilePath,
    JsonSerializerContext? _jsonCtx,
    StorageCacheOptions? _cacheOptions = null,
    StorageRetentionOptions? _retentionOptions = null)
  {
    p_jsonCtx = _jsonCtx;

    var connectionString = $"Data Source={_dbFilePath};";
    p_accessSemaphore = ToDispose(new SemaphoreSlim(1, 1));
    ToDoOnEnded(() => SqliteConnection.ClearAllPools());
    p_connection = ToDispose(new SqliteConnection(connectionString));
    p_connection.Open();

    p_accessSemaphore.Wait();

    using (var command = p_connection.CreateCommand())
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
    using (var cmd = p_connection.CreateCommand())
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

    p_accessSemaphore.Release();

    if (_cacheOptions != null)
    {
      var cacheMaxValues = _cacheOptions.CacheCapacity - _cacheOptions.CacheCapacity / 10;
      var cacheOverhead = _cacheOptions.CacheCapacity / 10;
      p_cache = new SyncCache<CacheKey, object>(new SyncCacheSettings(cacheMaxValues, cacheOverhead, _cacheOptions.CacheTTL));
    }

    if (_retentionOptions != null)
    {
      ToDisposeOnEnded(SharedPool<EventLoopScheduler>.Get(out var scheduler));

      var subs = Observable
        .Interval(_retentionOptions.ScanInterval ?? TimeSpan.FromMinutes(10), scheduler)
        .StartWithDefault()
        .ObserveOn(scheduler)
        .SelectAsync(async (_, _ct) =>
        {
          var now = DateTimeOffset.UtcNow;
          var docsToDeleteBuilder = ImmutableHashSet.CreateBuilder<DocumentEntryMeta>();

          await foreach (var doc in ListDocumentsMetaAsync((string?)null, _ct: _ct).ConfigureAwait(false))
          {
            var docAge = now - doc.Created;
            var docLastModifiedAge = now - doc.LastModified;
            if (_retentionOptions.DocumentMaxAgeFromCreation != null && docAge > _retentionOptions.DocumentMaxAgeFromCreation)
              docsToDeleteBuilder.Add(doc);
            else if (_retentionOptions.DocumentMaxAgeFromLastChange != null && docLastModifiedAge > _retentionOptions.DocumentMaxAgeFromLastChange)
              docsToDeleteBuilder.Add(doc);
          }

          foreach (var doc in docsToDeleteBuilder)
          {
            await DeleteDocumentsAsync(doc.Namespace, doc.Key, null, null, _ct);
            RemoveEntriesFromCache(doc.Namespace, doc.Key);
          }

          if (docsToDeleteBuilder.Count > 0 && _retentionOptions.OnDocsDeleteCallback != null)
          {
            try
            {
              var hashSet = docsToDeleteBuilder.ToImmutable();
              _retentionOptions.OnDocsDeleteCallback.Invoke(hashSet);
            }
            catch { }
          }
        }, scheduler)
        .Subscribe();

      ToDispose(subs);
    }
  }

  private async Task<DocumentEntry<T>> WriteDocumentInternalAsync<T>(
    string _namespace,
    string _key,
    T _data,
    JsonTypeInfo _jsonTypeInfo,
    CancellationToken _ct)
  {
    var now = DateTimeOffset.UtcNow;
    var json = JsonSerializer.Serialize(_data, _jsonTypeInfo);

    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      var insertSql =
        $"INSERT OR REPLACE INTO document_data (doc_id, namespace, key, last_modified, created, version, data) " +
        $"VALUES (@doc_id, @namespace, @key, @last_modified, @created, @version, @data) " +
        $"ON CONFLICT (namespace, key) " +
        $"DO UPDATE SET " +
        $"  last_modified=@last_modified, " +
        $"  version=version+1, " +
        $"  data=@data " +
        $"RETURNING doc_id, version, created; ";

      await using var command = p_connection.CreateCommand();
      command.CommandText = insertSql;
      command.Parameters.AddWithValue("@doc_id", Interlocked.Increment(ref p_documentsCounter));
      command.Parameters.AddWithValue("@namespace", _namespace);
      command.Parameters.AddWithValue("@key", _key);
      command.Parameters.AddWithValue("@last_modified", now.UtcTicks);
      command.Parameters.AddWithValue("@created", now.UtcTicks);
      command.Parameters.AddWithValue("@version", 1);
      command.Parameters.AddWithValue("@data", json);

      await using var reader = await command.ExecuteReaderAsync(_ct);
      if (await reader.ReadAsync(_ct))
      {
        var docId = reader.GetInt32(0);
        var version = reader.GetInt64(1);
        var created = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
        return new DocumentEntry<T>(docId, _namespace, _key, now, created, version, _data);
      }
    }
    finally
    {
      p_accessSemaphore.Release();
    }

    throw new InvalidOperationException($"Can't create document - db reader returned no result");
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public async Task<DocumentEntry<T>> WriteDocumentAsync<T>(
    string _namespace,
    string _key,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    var document = await WriteDocumentInternalAsync(_namespace, _key, _data, _jsonTypeInfo, _ct);
    p_cache?.Put(new CacheKey(_namespace, _key), document);
    return document;
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public Task<DocumentEntry<T>> WriteDocumentAsync<T>(
    string _namespace,
    int _key,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    return WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _data, _jsonTypeInfo, _ct);
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public async Task<DocumentEntry<T>> WriteDocumentAsync<T>(
    string _namespace,
    string _key,
    T _data,
    CancellationToken _ct) where T : notnull
  {
    if (p_jsonCtx == null)
      throw new InvalidOperationException($"You must specify a JsonSerializerContext in constructor");

    var typeInfo = p_jsonCtx.GetTypeInfo(typeof(T));
    if (typeInfo == null)
      throw new InvalidOperationException($"Type '{typeof(T).Name}' is not found in JsonSerializerContext!");

    var document = await WriteDocumentInternalAsync(_namespace, _key, _data, typeInfo, _ct);
    p_cache?.Put(new CacheKey(_namespace, _key), document);
    return document;
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public Task<DocumentEntry<T>> WriteDocumentAsync<T>(
    string _namespace,
    int _key,
    T _data,
    CancellationToken _ct) where T : notnull
  {
    return WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _data, _ct);
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(
    string _entryId,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return WriteDocumentAsync(ns, _entryId, _data, _jsonTypeInfo, _ct);
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(
    int _entryId,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return WriteDocumentAsync(ns, _entryId, _data, _jsonTypeInfo, _ct);
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(
    string _entryId,
    T _data,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return WriteDocumentAsync(ns, _entryId, _data, _ct);
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(
    int _entryId,
    T _data,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return await WriteDocumentAsync(ns, _entryId, _data, _ct);
  }

  /// <summary>
  /// Delete document from the database
  /// </summary>
  public async Task DeleteDocumentsAsync(
    string _namespace,
    string? _key,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    CancellationToken _ct = default)
  {
    var deleteSql =
      $"DELETE FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  (@key IS NULL OR @key=key) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var cmd = p_connection.CreateCommand();
      cmd.CommandText = deleteSql;
      cmd.Parameters.AddWithValue("@namespace", _namespace);

      if (_key != null)
        cmd.Parameters.AddWithValue("@key", _key);
      else
        cmd.Parameters.AddWithValue("@key", DBNull.Value);

      if (_from != null)
        cmd.Parameters.AddWithValue("@from", _from.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@from", DBNull.Value);

      if (_to != null)
        cmd.Parameters.AddWithValue("@to", _to.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@to", DBNull.Value);

      await cmd.ExecuteNonQueryAsync(_ct);
    }
    finally
    {
      RemoveEntriesFromCache(_namespace, _key);
      p_accessSemaphore.Release();
    }
  }

  public Task DeleteDocumentsAsync(
    string _namespace,
    int? _key,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    CancellationToken _ct = default)
  {
    return DeleteDocumentsAsync(_namespace, _key?.ToString(CultureInfo.InvariantCulture), _from, _to, _ct);
  }

  /// <summary>
  /// Delete document from the database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    await DeleteDocumentsAsync(ns, _entryId, null, null, _ct);
  }

  /// <summary>
  /// Delete document from the database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    await DeleteDocumentsAsync(ns, _entryId, null, null, _ct);
  }

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  public async IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    var listSql =
      $"SELECT doc_id, namespace, key, last_modified, created, version " +
      $"FROM document_data " +
      $"WHERE " +
      $"  (@namespace IS NULL OR @namespace=namespace) AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var cmd = p_connection.CreateCommand();
      cmd.CommandText = listSql;

      if (_namespace != null)
        cmd.Parameters.AddWithValue("@namespace", _namespace);
      else
        cmd.Parameters.AddWithValue("@namespace", DBNull.Value);

      if (_keyLikeExpression != null)
        cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression.Pattern);
      else
        cmd.Parameters.AddWithValue("@key_like", DBNull.Value);

      if (_from != null)
        cmd.Parameters.AddWithValue("@from", _from.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@from", DBNull.Value);

      if (_to != null)
        cmd.Parameters.AddWithValue("@to", _to.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@to", DBNull.Value);

      await using var reader = await cmd.ExecuteReaderAsync(_ct);
      while (await reader.ReadAsync(_ct))
      {
        var docId = reader.GetInt32(0);
        var optionalKey = reader.GetString(2);
        var lastModified = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
        var created = new DateTimeOffset(reader.GetInt64(4), TimeSpan.Zero);
        var version = reader.GetInt64(5);

        yield return new DocumentEntryMeta(docId, _namespace ?? reader.GetString(1), optionalKey, lastModified, created, version);
      }
    }
    finally
    {
      p_accessSemaphore.Release();
    }
  }

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  public async IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    var listSql =
      $"SELECT doc_id, namespace, key, last_modified, created, version " +
      $"FROM document_data " +
      $"WHERE " +
      $"  (@namespace_like IS NULL OR namespace LIKE @namespace_like) AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var cmd = p_connection.CreateCommand();
      cmd.CommandText = listSql;

      if (_namespaceLikeExpression != null)
        cmd.Parameters.AddWithValue("@namespace_like", _namespaceLikeExpression.Pattern);
      else
        cmd.Parameters.AddWithValue("@namespace_like", DBNull.Value);

      if (_keyLikeExpression != null)
        cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression.Pattern);
      else
        cmd.Parameters.AddWithValue("@key_like", DBNull.Value);

      if (_from != null)
        cmd.Parameters.AddWithValue("@from", _from.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@from", DBNull.Value);

      if (_to != null)
        cmd.Parameters.AddWithValue("@to", _to.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@to", DBNull.Value);

      await using var reader = await cmd.ExecuteReaderAsync(_ct);
      while (await reader.ReadAsync(_ct))
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
    finally
    {
      p_accessSemaphore.Release();
    }
  }

  /// <summary>
  /// List documents
  /// </summary>
  public async IAsyncEnumerable<DocumentEntry<T>> ListDocumentsAsync<T>(
    string _namespace,
    JsonTypeInfo _jsonTypeInfo,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    var listSql =
      $"SELECT doc_id, key, last_modified, created, version, data " +
      $"FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like) AND " +
      $"  (@from IS NULL OR last_modified>=@from) AND " +
      $"  (@to IS NULL OR last_modified<=@to); ";

    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var cmd = p_connection.CreateCommand();
      cmd.CommandText = listSql;

      cmd.Parameters.AddWithValue("@namespace", _namespace);

      if (_keyLikeExpression != null)
        cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression.Pattern);
      else
        cmd.Parameters.AddWithValue("@key_like", DBNull.Value);

      if (_from != null)
        cmd.Parameters.AddWithValue("@from", _from.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@from", DBNull.Value);

      if (_to != null)
        cmd.Parameters.AddWithValue("@to", _to.Value.UtcTicks);
      else
        cmd.Parameters.AddWithValue("@to", DBNull.Value);

      await using var reader = await cmd.ExecuteReaderAsync(_ct);
      while (await reader.ReadAsync(_ct))
      {
        var docId = reader.GetInt32(0);
        var key = reader.GetString(1);
        var lastModified = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
        var created = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
        var version = reader.GetInt64(4);
        var data = (T?)JsonSerializer.Deserialize(reader.GetString(5), _jsonTypeInfo);
        if (data == null)
          throw new FormatException($"Data of document '{docId}' is malformed!");

        yield return new DocumentEntry<T>(docId, _namespace, key, lastModified, created, version, data);
      }
    }
    finally
    {
      p_accessSemaphore.Release();
    }
  }

  /// <summary>
  /// List documents
  /// </summary>
  public IAsyncEnumerable<DocumentEntry<T>> ListDocumentsAsync<T>(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    CancellationToken _ct = default)
  {
    if (p_jsonCtx == null)
      throw new InvalidOperationException($"You must specify a JsonSerializerContext in constructor");

    var typeInfo = p_jsonCtx.GetTypeInfo(typeof(T));
    if (typeInfo == null)
      throw new InvalidOperationException($"Type '{typeof(T).Name}' is not found in JsonSerializerContext!");

    return ListDocumentsAsync<T>(_namespace, typeInfo, _keyLikeExpression, _from, _to, _ct);
  }

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public IAsyncEnumerable<DocumentEntry<T>> ListSimpleDocumentsAsync<T>(
    JsonTypeInfo<T> _jsonTypeInfo,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    CancellationToken _ct = default)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ListDocumentsAsync<T>(ns, _jsonTypeInfo, _keyLikeExpression, _from, _to, _ct);
  }

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public IAsyncEnumerable<DocumentEntry<T>> ListSimpleDocumentsAsync<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    CancellationToken _ct = default)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ListDocumentsAsync<T>(ns, _keyLikeExpression, _from, _to, _ct);
  }

  private async Task<DocumentEntry<T>?> ReadDocumentInternalAsync<T>(
    string _namespace,
    string _key,
    JsonTypeInfo _jsonTypeInfo,
    CancellationToken _ct)
  {
    var readSql =
      $"SELECT doc_id, key, last_modified, created, version, data " +
      $"FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  key=@key; ";

    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      using var cmd = p_connection.CreateCommand();
      cmd.CommandText = readSql;
      cmd.Parameters.AddWithValue("@namespace", _namespace);
      cmd.Parameters.AddWithValue("@key", _key);

      await using var reader = await cmd.ExecuteReaderAsync(_ct);
      if (await reader.ReadAsync(_ct))
      {
        var docId = reader.GetInt32(0);
        var optionalKey = reader.GetString(1);
        var lastModified = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
        var created = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
        var version = reader.GetInt64(4);
        var json = reader.GetString(5);
        var data = (T?)JsonSerializer.Deserialize(json, _jsonTypeInfo);

        if (data == null)
          throw new FormatException($"Data of document '{docId}' is malformed!");

        return new DocumentEntry<T>(docId, _namespace, optionalKey, lastModified, created, version, data);
      }

      return null;
    }
    finally
    {
      p_accessSemaphore.Release();
    }
  }

  /// <summary>
  /// Read document from the database
  /// </summary>
  public async Task<DocumentEntry<T>?> ReadDocumentAsync<T>(
    string _namespace,
    string _key,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct)
  {
    if (p_cache?.TryGet(new CacheKey(_namespace, _key), out var cachedValue) == true)
      return cachedValue as DocumentEntry<T>;

    var result = await ReadDocumentInternalAsync<T>(_namespace, _key, _jsonTypeInfo, _ct);

    p_cache?.Put(new CacheKey(_namespace, _key), result);

    return result;
  }

  /// <summary>
  /// Read document from the database
  /// </summary>
  public async Task<DocumentEntry<T>?> ReadDocumentAsync<T>(
    string _namespace,
    int _key,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct)
  {
    return await ReadDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _jsonTypeInfo, _ct);
  }

  /// <summary>
  /// Read document from the database
  /// </summary>
  public async Task<DocumentEntry<T>?> ReadDocumentAsync<T>(
    string _namespace,
    string _key,
    CancellationToken _ct)
  {
    if (p_cache?.TryGet(new CacheKey(_namespace, _key), out var cachedValue) == true)
      return cachedValue as DocumentEntry<T>;

    if (p_jsonCtx == null)
      throw new InvalidOperationException($"You must specify a JsonSerializerContext in constructor");

    var typeInfo = p_jsonCtx.GetTypeInfo(typeof(T));
    if (typeInfo == null)
      throw new InvalidOperationException($"Type '{typeof(T).Name}' is not found in JsonSerializerContext!");

    var result = await ReadDocumentInternalAsync<T>(_namespace, _key, typeInfo, _ct);

    p_cache?.Put(new CacheKey(_namespace, _key), result);

    return result;
  }

  /// <summary>
  /// Read document from the database
  /// </summary>
  public Task<DocumentEntry<T>?> ReadDocumentAsync<T>(
    string _namespace,
    int _key,
    CancellationToken _ct)
  {
    return ReadDocumentAsync<T>(_namespace, _key.ToString(CultureInfo.InvariantCulture), _ct);
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(
    string _entryId,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return await ReadDocumentAsync(ns, _entryId, _jsonTypeInfo, _ct);
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(
    int _entryId,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return await ReadDocumentAsync(ns, _entryId, _jsonTypeInfo, _ct);
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(
    string _entryId,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ReadDocumentAsync<T>(ns, _entryId, _ct);
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(
    int _entryId,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ReadDocumentAsync<T>(ns, _entryId, _ct);
  }

  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  public async Task CompactDatabase(CancellationToken _ct)
  {
    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var command = p_connection.CreateCommand();
      command.CommandText = "VACUUM;";
      await command.ExecuteNonQueryAsync(_ct);
    }
    finally
    {
      p_accessSemaphore.Release();
    }
  }

  /// <summary>
  /// Flushes temporary file to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  public async Task FlushAsync(bool _force, CancellationToken _ct)
  {
    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var command = p_connection.CreateCommand();
      command.CommandText = $"PRAGMA wal_checkpoint({(_force ? "TRUNCATE" : "PASSIVE")});";
      await command.ExecuteNonQueryAsync(_ct);
    }
    finally
    {
      p_accessSemaphore.Release();
    }
  }

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  public async Task<int> Count(
    string? _namespace,
    LikeExpr? _keyLikeExpression,
    CancellationToken _ct)
  {
    var readSql =
      $"SELECT COUNT(*) " +
      $"FROM document_data " +
      $"WHERE " +
      $"  (@namespace IS NULL OR @namespace=namespace) AND " +
      $"  (@key_like IS NULL OR key LIKE @key_like); ";

    await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var cmd = p_connection.CreateCommand();
      cmd.CommandText = readSql;

      if (_namespace != null)
        cmd.Parameters.AddWithValue("@namespace", _namespace);
      else
        cmd.Parameters.AddWithValue("@namespace", DBNull.Value);

      if (_keyLikeExpression != null)
        cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression.Pattern);
      else
        cmd.Parameters.AddWithValue("@key_like", DBNull.Value);

      await using var reader = await cmd.ExecuteReaderAsync(_ct);
      if (await reader.ReadAsync(_ct))
      {
        var count = reader.GetInt32(0);
        return count;
      }

      return 0;
    }
    finally
    {
      p_accessSemaphore.Release();
    }
  }

  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  public async Task<int> CountSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression,
    CancellationToken _ct)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return await Count(ns, _keyLikeExpression, _ct);
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

}
