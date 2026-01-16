using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Microsoft.Data.Sqlite;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Ax.Fw.Storage;

public class SqliteBlobStorage : DisposableStack, IBlobStorage
{
  record CacheKey(string Namespace, string Key);

  private readonly string p_dbFilePath;
  private readonly SemaphoreSlim p_writeSemaphore;
  private long p_documentsCounter = 0;

  /// <summary>
  /// Opens existing database or creates new
  /// </summary>
  /// <param name="_dbFilePath">Path to database file</param>
  public SqliteBlobStorage(
    string _dbFilePath,
    StorageRetentionOptions? _retentionOptions = null)
  {
    p_dbFilePath = _dbFilePath;
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
          $"  data BLOB NOT NULL, " +
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
            DeleteDocuments(doc.Namespace, doc.Key, null, null);

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

  private async Task<DocumentEntryMeta> WriteDocumentInternalAsync(
    string _namespace,
    string _key,
    Stream _data,
    CancellationToken _ct)
  {
    const string insertSql =
      $"INSERT OR REPLACE INTO document_data (doc_id, namespace, key, last_modified, created, version, data) " +
      $"VALUES (@doc_id, @namespace, @key, @last_modified, @created, @version, zeroblob(@length)) " +
      $"ON CONFLICT (namespace, key) " +
      $"DO UPDATE SET " +
      $"  last_modified=@last_modified, " +
      $"  version=version+1, " +
      $"  data=zeroblob(@length) " +
      $"RETURNING doc_id, version, created; ";

    await p_writeSemaphore.WaitAsync(_ct);

    var now = DateTimeOffset.UtcNow;
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
      command.Parameters.AddWithValue("@length", _data.Length);

      using var reader = command.ExecuteReader();
      if (!reader.Read())
        throw new InvalidOperationException($"Can't create document - db reader returned no result");

      var docId = reader.GetInt32(0);
      var version = reader.GetInt64(1);
      var created = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);

      using (var writeStream = new SqliteBlob(connection, "document_data", "data", docId))
        await _data.CopyToAsync(writeStream, _ct);

      return new DocumentEntryMeta(docId, _namespace, _key, now, created, version);
    }
    finally
    {
      p_writeSemaphore.Release();
    }
  }

  /// <summary>
  /// Upsert blob to database
  /// </summary>
  public async Task<DocumentEntryMeta> WriteDocumentAsync(
    string _namespace,
    KeyAlike _key,
    Stream _data,
    CancellationToken _ct)
  {
    var document = await WriteDocumentInternalAsync(_namespace, _key.Key, _data, _ct);
    return document;
  }

  /// <summary>
  /// Upsert blob to database
  /// </summary>
  public async Task<DocumentEntryMeta> WriteDocumentAsync(
    string _namespace,
    KeyAlike _key,
    byte[] _data,
    CancellationToken _ct)
  {
    using var ms = new MemoryStream(_data);
    var document = await WriteDocumentInternalAsync(_namespace, _key.Key, ms, _ct);
    return document;
  }

  /// <summary>
  /// Delete blobs from the database
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

    using var cmd = connection.CreateCommand();
    cmd.CommandText = deleteSql;
    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithNullableValue("@key", _key?.Key);
    cmd.Parameters.AddWithNullableValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithNullableValue("@to", _to?.UtcTicks);

    cmd.ExecuteNonQuery();
  }

  /// <summary>
  /// List blobs meta info
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
  /// List blobs meta info
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

  private async Task<DocumentEntryMeta?> ReadDocumentInternalAsync(
    string _namespace,
    string _key,
    Stream _outputStream,
    CancellationToken _ct)
  {
    const string readSql =
      $"SELECT doc_id, key, last_modified, created, version " +
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

      using (var blob = new SqliteBlob(connection, "document_data", "data", docId, readOnly: true))
        await blob.CopyToAsync(_outputStream, _ct);

      return new DocumentEntryMeta(docId, _namespace, optionalKey, lastModified, created, version);
    }

    return null;
  }

  /// <summary>
  /// Read blob from the database
  /// </summary>
  public async Task<DocumentEntryMeta?> ReadDocumentAsync(
    string _namespace,
    KeyAlike _key,
    Stream _outputStream,
    CancellationToken _ct)
  {
    var result = await ReadDocumentInternalAsync(_namespace, _key.Key, _outputStream, _ct);
    return result;
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
  /// Returns number of blobs in database
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

  private SqliteConnection GetConnection()
  {
    var connection = new SqliteConnection($"Data Source={p_dbFilePath};");
    connection.Open();
    return connection;
  }

}