using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Data.Retention;
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
  private readonly string p_dbFilePath;
  private readonly SemaphoreSlim p_writeSemaphore;

  /// <summary>
  /// Initializes a new instance of the <see cref="SqliteBlobStorage"/> class.
  /// Opens existing database or creates new one with automatic retention policy support.
  /// </summary>
  /// <param name="_dbFilePath">Path to the SQLite database file</param>
  /// <param name="_retentionOptions">Optional retention policy configuration for automatic document cleanup</param>
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
          var removedDocs = _retentionOptions.Rules
            .SelectMany(_ => _.Apply(this))
            .ToHashSet();

          if (removedDocs.Count > 0 && _retentionOptions.OnDocsDeleteCallback != null)
          {
            try
            {
              _retentionOptions.OnDocsDeleteCallback.Invoke(removedDocs);
            }
            catch { }
          }
        });

      ToDispose(subs);
    }
  }

  /// <summary>
  /// Gets the file system path to the database file.
  /// </summary>
  public string FilePath => p_dbFilePath;

  /// <summary>
  /// Writes a blob to the database from a stream using SQLite's incremental I/O.
  /// If a blob with the same namespace and key exists, it will be updated.
  /// </summary>
  /// <param name="_namespace">The namespace for organizing blobs</param>
  /// <param name="_key">The unique key within the namespace</param>
  /// <param name="_data">The stream containing blob data to write</param>
  /// <param name="_size">The total size of the blob in bytes</param>
  /// <param name="_ct">Cancellation token</param>
  /// <returns>Metadata about the written blob entry</returns>
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
  /// Writes a blob to the database from a byte array.
  /// If a blob with the same namespace and key exists, it will be updated.
  /// </summary>
  /// <param name="_namespace">The namespace for organizing blobs</param>
  /// <param name="_key">The unique key within the namespace</param>
  /// <param name="_data">The byte array containing blob data to write</param>
  /// <param name="_ct">Cancellation token</param>
  /// <returns>Metadata about the written blob entry</returns>
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
  /// Writes a blob to the database from a byte array.
  /// If a blob with the same namespace and key exists, it will be updated.
  /// </summary>
  /// <param name="_namespace">The namespace for organizing blobs</param>
  /// <param name="_key">The unique key within the namespace</param>
  /// <param name="_data">The byte array containing blob data to write</param>
  /// <returns>Metadata about the written blob entry</returns>
  public BlobEntryMeta WriteBlob(
    string _namespace,
    KeyAlike _key,
    byte[] _data)
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

    p_writeSemaphore.Wait();

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
  /// Deletes blobs from the database based on specified criteria.
  /// All parameters except namespace can be null to delete all matching blobs.
  /// </summary>
  /// <param name="_namespace">The namespace to delete blobs from</param>
  /// <param name="_key">Optional specific key to delete. If null, all keys in namespace are considered</param>
  /// <param name="_from">Optional start time filter based on last modified timestamp</param>
  /// <param name="_to">Optional end time filter based on last modified timestamp</param>
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
  /// Lists metadata for blobs in a specific namespace with optional filtering.
  /// </summary>
  /// <param name="_namespace">The namespace to query. If null, all namespaces are included</param>
  /// <param name="_keyLikeExpression">Optional SQL LIKE expression to filter keys</param>
  /// <param name="_from">Optional start time filter based on last modified timestamp</param>
  /// <param name="_to">Optional end time filter based on last modified timestamp</param>
  /// <returns>Enumerable collection of blob metadata entries</returns>
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
  /// Lists metadata for blobs with optional filtering by both namespace and key patterns.
  /// </summary>
  /// <param name="_namespaceLikeExpression">Optional SQL LIKE expression to filter namespaces</param>
  /// <param name="_keyLikeExpression">Optional SQL LIKE expression to filter keys</param>
  /// <param name="_from">Optional start time filter based on last modified timestamp</param>
  /// <param name="_to">Optional end time filter based on last modified timestamp</param>
  /// <returns>Enumerable collection of blob metadata entries</returns>
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
  /// Lists blobs from that match the specified criteria, transforming each blob's data using the
  /// provided function.
  /// </summary>
  /// <remarks>Enumeration is deferred; blobs are retrieved and transformed as the returned sequence is
  /// iterated. The caller is responsible for ensuring that the provided transformation function correctly processes the
  /// blob data stream. This method is not thread-safe.</remarks>
  /// <typeparam name="T">The type of the transformed data returned for each blob entry.</typeparam>
  /// <param name="_namespace">The namespace to filter blobs by. If <paramref name="_namespace"/> is <see langword="null"/>, blobs from all
  /// namespaces are included.</param>
  /// <param name="_dataTransformFunc">A function that transforms the raw blob data stream and associated metadata into an object of type <typeparamref
  /// name="T"/>. The function receives the blob's data stream and a tuple containing the document ID and raw data
  /// length.</param>
  /// <param name="_keyLikeExpression">An optional pattern used to filter blobs by key. If <paramref name="_keyLikeExpression"/> is <see
  /// langword="null"/>, all keys are included.</param>
  /// <param name="_from">An optional lower bound for the blob's last modified timestamp. Only blobs modified on or after this time are
  /// included. If <paramref name="_from"/> is <see langword="null"/>, no lower bound is applied.</param>
  /// <param name="_to">An optional upper bound for the blob's last modified timestamp. Only blobs modified on or before this time are
  /// included. If <paramref name="_to"/> is <see langword="null"/>, no upper bound is applied.</param>
  /// <returns>An enumerable collection of <see cref="BlobEntry{T}"/> objects representing the blobs that match the specified
  /// filters. Each entry contains the transformed data and associated metadata.</returns>
  public IEnumerable<BlobEntry<T>> ListBlobs<T>(
    string? _namespace,
    Func<Stream, (long DocId, long RawLength), T> _dataTransformFunc,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    const string listSql =
      $"SELECT doc_id, namespace, key, last_modified, created, version, length(data) as length " +
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
      var ns = (string)reader["namespace"];
      var optionalKey = reader.GetString(2);
      var lastModified = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
      var created = new DateTimeOffset(reader.GetInt64(4), TimeSpan.Zero);
      var version = reader.GetInt64(5);
      var length = (long)reader["length"];

      using var sqliteBlob = new SqliteBlob(connection, "document_data", "data", docId, true);
      var data = _dataTransformFunc(sqliteBlob, (docId, length));

      yield return new BlobEntry<T>(docId, ns, optionalKey, lastModified, created, version, length, data);
    }
  }

  /// <summary>
  /// Attempts to read a blob from the database as a stream.
  /// Caller is responsible for disposing the returned <see cref="BlobStream"/>.
  /// </summary>
  /// <param name="_namespace">The namespace containing the blob</param>
  /// <param name="_key">The key of the blob to read</param>
  /// <param name="_outputData">Output parameter containing the blob stream if found</param>
  /// <param name="_meta">Output parameter containing blob metadata if found</param>
  /// <returns>True if the blob was found, false otherwise</returns>
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
  /// Attempts to read a blob from the database as a byte array.
  /// </summary>
  /// <param name="_namespace">The namespace containing the blob</param>
  /// <param name="_key">The key of the blob to read</param>
  /// <param name="_outputData">Output parameter containing the blob data as byte array if found</param>
  /// <param name="_meta">Output parameter containing blob metadata if found</param>
  /// <returns>True if the blob was found, false otherwise</returns>
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
  /// Rebuilds the database file, repacking it into a minimal amount of disk space.
  /// This operation runs VACUUM command which can be time-consuming for large databases.
  /// </summary>
  public void CompactDatabase()
  {
    using var connection = GetConnection();
    using var command = connection.CreateCommand();
    command.CommandText = "VACUUM;";
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Flushes the Write-Ahead Log (WAL) to the main database file.
  /// </summary>
  /// <param name="_force">If true, performs a full checkpoint and truncates the WAL file to zero bytes. If false, performs a passive checkpoint.</param>
  public void Flush(bool _force)
  {
    using var connection = GetConnection();
    using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA wal_checkpoint({(_force ? "TRUNCATE" : "PASSIVE")});";
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Returns the total number of blobs in the database matching the specified criteria.
  /// </summary>
  /// <param name="_namespace">The namespace to count blobs in. If null, all namespaces are included</param>
  /// <param name="_keyLikeExpression">Optional SQL LIKE expression to filter keys</param>
  /// <returns>The count of matching blobs</returns>
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

  /// <summary>
  /// Creates and opens a new SQLite connection to the database.
  /// </summary>
  /// <returns>An open SQLite connection</returns>
  private SqliteConnection GetConnection()
  {
    var connection = new SqliteConnection($"Data Source={p_dbFilePath};");
    connection.Open();
    return connection;
  }

}
