using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Ax.Fw.Storage;

#if NET8_0_OR_GREATER
public class SqliteDocumentStorageAot : DisposableStack, IDocumentStorageAot
{
  private readonly SemaphoreSlim p_accessSemaphore;
  private readonly SqliteConnection p_connection;
  private long p_documentsCounter = 0;

  public SqliteDocumentStorageAot(string _dbFilePath)
  {
    var connectionString = $"Data Source={_dbFilePath};";
    p_accessSemaphore = ToDispose(new SemaphoreSlim(1, 1));
    p_connection = ToDispose(new SqliteConnection(connectionString));
    p_connection.Open();

    p_accessSemaphore.Wait();

    using var command = p_connection.CreateCommand();
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

    p_documentsCounter = GetLatestDocumentId();

    p_accessSemaphore.Release();
  }

  public async Task<DocumentEntry<T>> WriteDocumentAsync<T>(
    string _namespace,
    string _key,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
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

  public async Task<DocumentEntry<T>> WriteDocumentAsync<T>(
    string _namespace,
    int _key,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    return await WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _data, _jsonTypeInfo, _ct);
  }

  public async Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(
    string _entryId,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();

    return await WriteDocumentAsync(ns, _entryId, _data, _jsonTypeInfo, _ct);
  }

  public async Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(
    int _entryId,
    T _data,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    return await WriteSimpleDocumentAsync(_entryId.ToString(CultureInfo.InvariantCulture), _data, _jsonTypeInfo, _ct);
  }

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
      p_accessSemaphore.Release();
    }
  }

  public async Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();

    await DeleteDocumentsAsync(ns, _entryId, null, null, _ct);
  }

  public async Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
  {
    await DeleteSimpleDocumentAsync<T>(_entryId.ToString(CultureInfo.InvariantCulture), _ct);
  }

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

    //await p_accessSemaphore.WaitAsync(_ct);
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
      //p_accessSemaphore.Release();
    }
  }

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

    //await p_accessSemaphore.WaitAsync(_ct);
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
      //p_accessSemaphore.Release();
    }
  }

  public async IAsyncEnumerable<DocumentEntry<T>> ListDocumentsAsync<T>(
    string _namespace,
    JsonTypeInfo<T> _jsonTypeInfo,
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

    //await p_accessSemaphore.WaitAsync(_ct);
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
        var data = JsonSerializer.Deserialize(reader.GetString(5), _jsonTypeInfo);
        if (data == null)
          throw new FormatException($"Data of document '{docId}' is malformed!");

        yield return new DocumentEntry<T>(docId, _namespace, key, lastModified, created, version, data);
      }
    }
    finally
    {
      //p_accessSemaphore.Release();
    }
  }

  public IAsyncEnumerable<DocumentEntry<T>> ListSimpleDocumentsAsync<T>(
    JsonTypeInfo<T> _jsonTypeInfo,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    CancellationToken _ct = default)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ListDocumentsAsync(ns, _jsonTypeInfo, _keyLikeExpression, _from, _to, _ct);
  }

  public async Task<DocumentEntry<T>?> ReadDocumentAsync<T>(
    string _namespace,
    string _key,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct)
  {
    var readSql =
      $"SELECT doc_id, key, last_modified, created, version, data " +
      $"FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  key=@key; ";

    //await p_accessSemaphore.WaitAsync(_ct);
    try
    {
      await using var cmd = p_connection.CreateCommand();
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
        var data = JsonSerializer.Deserialize(json, _jsonTypeInfo);

        if (data == null)
          throw new FormatException($"Data of document '{docId}' is malformed!");

        return new DocumentEntry<T>(docId, _namespace, optionalKey, lastModified, created, version, data);
      }

      return null;
    }
    finally
    {
      //p_accessSemaphore.Release();
    }
  }

  public async Task<DocumentEntry<T>?> ReadDocumentAsync<T>(
    string _namespace,
    int _key,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct)
  {
    return await ReadDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _jsonTypeInfo, _ct);
  }

  public async Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(
    string _entryId,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    var document = await ReadDocumentAsync<T>(ns, _entryId, _jsonTypeInfo, _ct);
    return document;
  }

  public async Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(
    int _entryId,
    JsonTypeInfo<T> _jsonTypeInfo,
    CancellationToken _ct) where T : notnull
  {
    return await ReadSimpleDocumentAsync(_entryId.ToString(CultureInfo.InvariantCulture), _jsonTypeInfo, _ct);
  }

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

    //await p_accessSemaphore.WaitAsync(_ct);
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
      //p_accessSemaphore.Release();
    }
  }

  public async Task<int> CountSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression,
    CancellationToken _ct)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return await Count(ns, _keyLikeExpression, _ct);
  }

  private long GetLatestDocumentId()
  {
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

    return counter;
  }

}
#endif