using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Microsoft.Data.Sqlite;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Ax.Fw.Storage;

public class SqliteBlobStorage : DisposableStack, IBlobStorage
{
  record CacheKey(string Namespace, string Key);

  private readonly string p_dbFilePath;
  private readonly SemaphoreSlim p_writeSemaphore;

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
          var docsToDelete = new HashSet<BlobEntryMeta>();

          foreach (var rule in _retentionOptions.Rules)
          {
            foreach (var doc in ListBlobsMeta(rule.Namespace))
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
            DeleteBlobs(doc.Namespace, doc.Key, null, null);

          if (docsToDelete.Count > 0 && _retentionOptions.OnDocsDeleteCallback != null)
          {
            try
            {
              var data = docsToDelete
                .Select(_ => new DocumentEntryMeta(_.DocId, _.Namespace, _.Key, _.LastModified, _.Created, _.Version))
                .ToHashSet();

              _retentionOptions.OnDocsDeleteCallback.Invoke(data);
            }
            catch { }
          }
        });

      ToDispose(subs);
    }
  }

  /// <summary>
  /// Upsert blob to database
  /// </summary>
  public async Task<BlobEntryMeta> WriteBlobAsync(
    string _namespace,
    KeyAlike _key,
    Stream _data,
    long _size,
    CancellationToken _ct)
  {
    const string insertSql =
      $"INSERT OR REPLACE INTO document_data (namespace, key, last_modified, created, version, data) " +
      $"VALUES (@namespace, @key, @last_modified, @created, @version, zeroblob(@size)) " +
      $"ON CONFLICT (namespace, key) " +
      $"DO UPDATE SET " +
      $"  last_modified=@last_modified, " +
      $"  version=version+1, " +
      $"  data=zeroblob(@size) " +
      $"RETURNING doc_id, version, created; ";

    await p_writeSemaphore.WaitAsync(_ct);

    var now = DateTimeOffset.UtcNow;
    try
    {
      using var connection = GetConnection();
      using var command = connection.CreateCommand();

      command.CommandText = insertSql;
      command.Parameters.AddWithValue("@namespace", _namespace);
      command.Parameters.AddWithValue("@key", _key.Key);
      command.Parameters.AddWithValue("@last_modified", now.UtcTicks);
      command.Parameters.AddWithValue("@created", now.UtcTicks);
      command.Parameters.AddWithValue("@version", 1);
      command.Parameters.AddWithValue("@size", _size);

      using var reader = command.ExecuteReader();
      if (!reader.Read())
        throw new InvalidOperationException($"Can't create document - db reader returned no result");

      var docId = reader.GetInt64(0);
      var version = reader.GetInt64(1);
      var created = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);

      using (var writeStream = new SqliteBlob(connection, "document_data", "data", docId))
      {
        var bufferLength = (int)Math.Min(_size, 81920);
        var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
        try
        {
          var totalRead = 0L;
          while (totalRead < _size)
          {
            var toRead = (int)Math.Min(bufferLength, _size - totalRead);
            await _data.ReadExactlyAsync(buffer, 0, toRead, _ct);
            await writeStream.WriteAsync(buffer.AsMemory(0, toRead), _ct);
            totalRead += toRead;
          }
        }
        finally
        {
          ArrayPool<byte>.Shared.Return(buffer);
        }
      }

      return new BlobEntryMeta(docId, _namespace, _key.Key, now, created, version, _size);
    }
    finally
    {
      p_writeSemaphore.Release();
    }
  }

  /// <summary>
  /// Upsert blob to database
  /// </summary>
  public async Task<BlobEntryMeta> WriteBlobAsync(
    string _namespace,
    KeyAlike _key,
    byte[] _data,
    CancellationToken _ct)
  {
    const string insertSql =
      $"INSERT OR REPLACE INTO document_data (namespace, key, last_modified, created, version, data) " +
      $"VALUES (@namespace, @key, @last_modified, @created, @version, @data) " +
      $"ON CONFLICT (namespace, key) " +
      $"DO UPDATE SET " +
      $"  last_modified=@last_modified, " +
      $"  version=version+1, " +
      $"  data=@data " +
      $"RETURNING doc_id, version, created; ";

    await p_writeSemaphore.WaitAsync(_ct);

    var now = DateTimeOffset.UtcNow;
    try
    {
      using var connection = GetConnection();
      using var command = connection.CreateCommand();

      command.CommandText = insertSql;
      command.Parameters.AddWithValue("@namespace", _namespace);
      command.Parameters.AddWithValue("@key", _key.Key);
      command.Parameters.AddWithValue("@last_modified", now.UtcTicks);
      command.Parameters.AddWithValue("@created", now.UtcTicks);
      command.Parameters.AddWithValue("@version", 1);
      command.Parameters.AddWithValue("@data", SqliteType.Blob, _data);

      using var reader = command.ExecuteReader();
      if (!reader.Read())
        throw new InvalidOperationException($"Can't create document - db reader returned no result");

      var docId = reader.GetInt64(0);
      var version = reader.GetInt64(1);
      var created = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);

      return new BlobEntryMeta(docId, _namespace, _key.Key, now, created, version, _data.Length);
    }
    finally
    {
      p_writeSemaphore.Release();
    }
  }

  /// <summary>
  /// Delete blobs from the database
  /// </summary>
  public void DeleteBlobs(
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
  public IEnumerable<BlobEntryMeta> ListBlobsMeta(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    const string listSql =
      $"SELECT doc_id, namespace, key, last_modified, created, version, length(data) " +
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
      var length = reader.GetInt64(6);

      yield return new BlobEntryMeta(docId, _namespace ?? reader.GetString(1), optionalKey, lastModified, created, version, length);
    }
  }

  /// <summary>
  /// List blobs meta info
  /// </summary>
  public IEnumerable<BlobEntryMeta> ListBlobsMeta(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    const string listSql =
      $"SELECT doc_id, namespace, key, last_modified, created, version, length(data) " +
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
      var length = reader.GetInt64(6);

      yield return new BlobEntryMeta(docId, ns, optionalKey, lastModified, created, version, length);
    }
  }

  /// <summary>
  /// Read blob from the database
  /// </summary>
  public bool TryReadBlob(
    string _namespace,
    KeyAlike _key,
    [NotNullWhen(true)] out BlobStream? _outputData,
    [NotNullWhen(true)] out BlobEntryMeta? _meta)
  {
    const string readSql =
      $"SELECT doc_id, key, last_modified, created, version, length(data) " +
      $"FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  key=@key; ";

    using var connection = GetConnection();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = readSql;
    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithValue("@key", _key.Key);

    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
      var docId = (long)reader["doc_id"];
      var optionalKey = (string)reader["key"];
      var lastModified = new DateTimeOffset((long)reader["last_modified"], TimeSpan.Zero);
      var created = new DateTimeOffset((long)reader["created"], TimeSpan.Zero);
      var version = (long)reader["version"];
      var length = reader.GetInt64(5);

      var blobConnection = GetConnection();
      try
      {
        var sqliteBlob = new SqliteBlob(blobConnection, "document_data", "data", docId, true);
        _outputData = new BlobStream(blobConnection, sqliteBlob);
        _meta = new BlobEntryMeta(docId, _namespace, optionalKey, lastModified, created, version, length);
        return true;
      }
      catch
      {
        blobConnection.Dispose();
        throw;
      }
    }

    _outputData = null;
    _meta = null;
    return false;
  }

  /// <summary>
  /// Read blob from the database
  /// </summary>
  public bool TryReadBlob(
    string _namespace,
    KeyAlike _key,
    [NotNullWhen(true)] out byte[]? _outputData,
    [NotNullWhen(true)] out BlobEntryMeta? _meta)
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
    cmd.Parameters.AddWithValue("@key", _key.Key);

    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
      var docId = (long)reader["doc_id"];
      var optionalKey = (string)reader["key"];
      var lastModified = new DateTimeOffset((long)reader["last_modified"], TimeSpan.Zero);
      var created = new DateTimeOffset((long)reader["created"], TimeSpan.Zero);
      var version = (long)reader["version"];
      var data = (byte[])reader["data"];

      _outputData = data;
      _meta = new BlobEntryMeta(docId, _namespace, optionalKey, lastModified, created, version, data.LongLength);
      return true;
    }

    _outputData = null;
    _meta = null;
    return false;
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
  public long Count(
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
      var count = reader.GetInt64(0);
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
