using Ax.Fw.SharedTypes.Attributes;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using System.Data.SQLite;
using System.Globalization;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Ax.Fw.Storage;

public class SqliteDocumentStorageV2 : DisposableStack, IDocumentStorage
{
  private readonly SQLiteConnection p_connection;
  private long p_documentsCounter = 0;

  /// <summary>
  /// Opens existing database or creates new
  /// </summary>
  /// <param name="_dbFilePath">Path to database file</param>
  public SqliteDocumentStorageV2(string _dbFilePath)
  {
    p_connection = ToDispose(new SQLiteConnection($"Data Source={_dbFilePath};Version=3;").OpenAndReturn());

    using var command = new SQLiteCommand(p_connection);
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
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public async Task<DocumentTypedEntry<T>> WriteDocumentAsync<T>(string _namespace, string _key, T _data, CancellationToken _ct) where T : notnull
  {
    var json = JsonSerializer.Serialize(_data);
    var now = DateTimeOffset.UtcNow;

    var insertSql =
      $"INSERT OR REPLACE INTO document_data (doc_id, namespace, key, last_modified, created, version, data) " +
      $"VALUES (@doc_id, @namespace, @key, @last_modified, @created, @version, @data) " +
      $"ON CONFLICT (namespace, key) " +
      $"DO UPDATE SET " +
      $"  last_modified=@last_modified, " +
      $"  version=version+1, " +
      $"  data=@data " +
      $"RETURNING doc_id, version, created; ";

    await using var command = new SQLiteCommand(p_connection);
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
      return new DocumentTypedEntry<T>(docId, _namespace, _key, now, created, version, _data);
    }

    throw new InvalidOperationException($"Can't create document - db reader returned no result");
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public async Task<DocumentTypedEntry<T>> WriteDocumentAsync<T>(string _namespace, int _key, T _data, CancellationToken _ct) where T : notnull
  {
    return await WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _data, _ct);
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task<DocumentTypedEntry<T>> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();

    return await WriteDocumentAsync(ns, _entryId, _data, _ct);
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task<DocumentTypedEntry<T>> WriteSimpleDocumentAsync<T>(int _entryId, T _data, CancellationToken _ct) where T : notnull
  {
    return await WriteSimpleDocumentAsync(_entryId.ToString(CultureInfo.InvariantCulture), _data, _ct);
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

    await using var cmd = new SQLiteCommand(p_connection);
    cmd.CommandText = deleteSql;
    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithValue("@key", _key);
    cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

    await cmd.ExecuteNonQueryAsync(_ct);
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
    await DeleteSimpleDocumentAsync<T>(_entryId.ToString(CultureInfo.InvariantCulture), _ct);
  }

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
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

    await using var cmd = new SQLiteCommand(p_connection);
    cmd.CommandText = listSql;
    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression?.Pattern);
    cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

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

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  /// <param name="_namespaceLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with namespace starting with "tel:123-456-")</param>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
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

    await using var cmd = new SQLiteCommand(p_connection);
    cmd.CommandText = listSql;
    cmd.Parameters.AddWithValue("@namespace_like", _namespaceLikeExpression?.Pattern);
    cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression?.Pattern);
    cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

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

  /// <summary>
  /// List documents
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  public async IAsyncEnumerable<DocumentTypedEntry<T>> ListDocumentsAsync<T>(
    string _namespace,
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

    await using var cmd = new SQLiteCommand(p_connection);
    cmd.CommandText = listSql;
    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression?.Pattern);
    cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
    cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

    await using var reader = await cmd.ExecuteReaderAsync(_ct);
    while (await reader.ReadAsync(_ct))
    {
      var docId = reader.GetInt32(0);
      var key = reader.GetString(1);
      var lastModified = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
      var created = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
      var version = reader.GetInt64(4);
      var data = JsonSerializer.Deserialize<T>(reader.GetString(5));
      if (data == null)
        throw new FormatException($"Data of document '{docId}' is malformed!");

      yield return new DocumentTypedEntry<T>(docId, _namespace, key, lastModified, created, version, data);
    }
  }

#pragma warning disable CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  public IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ListDocumentsAsync<T>(ns, _keyLikeExpression, _from, _to, _ct);
  }

#pragma warning restore CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable

  /// <summary>
  /// Read document from the database
  /// </summary>
  public async Task<DocumentTypedEntry<T>?> ReadDocumentAsync<T>(
    string _namespace,
    string _key,
    CancellationToken _ct)
  {
    var readSql =
      $"SELECT doc_id, key, last_modified, created, version, data " +
      $"FROM document_data " +
      $"WHERE " +
      $"  @namespace=namespace AND " +
      $"  key=@key; ";

    await using var cmd = new SQLiteCommand(p_connection);
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
      var data = JsonSerializer.Deserialize<T>(reader.GetString(5));
      if (data == null)
        throw new FormatException($"Data of document '{docId}' is malformed!");

      return new DocumentTypedEntry<T>(docId, _namespace, optionalKey, lastModified, created, version, data);
    }

    return null;
  }

  /// <summary>
  /// Read document from the database
  /// </summary>
  public async Task<DocumentTypedEntry<T>?> ReadDocumentAsync<T>(
      string _namespace,
      int _key,
      CancellationToken _ct)
  {
    return await ReadDocumentAsync<T>(_namespace, _key.ToString(CultureInfo.InvariantCulture), _ct);
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    var document = await ReadDocumentAsync<T>(ns, _entryId, _ct);
    return document;
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public async Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
  {
    return await ReadSimpleDocumentAsync<T>(_entryId.ToString(), _ct);
  }

  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  public async Task CompactDatabase(CancellationToken _ct)
  {
    await using var command = new SQLiteCommand(p_connection);
    command.CommandText = "VACUUM;";
    await command.ExecuteNonQueryAsync(_ct);
  }

  /// <summary>
  /// Flushes temporary file to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  public async Task FlushAsync(bool _force, CancellationToken _ct)
  {
    await using var command = new SQLiteCommand(p_connection);
    command.CommandText = $"PRAGMA wal_checkpoint({(_force ? "TRUNCATE" : "PASSIVE")});";
    await command.ExecuteNonQueryAsync(_ct);
  }

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  public async Task<int> Count(string? _namespace, LikeExpr? _keyLikeExpression, CancellationToken _ct)
  {
    var readSql =
        $"SELECT COUNT(*) " +
        $"FROM document_data " +
        $"WHERE " +
        $"  (@namespace IS NULL OR @namespace=namespace) AND " +
        $"  (@key_like IS NULL OR key LIKE @key_like); ";

    await using var cmd = new SQLiteCommand(p_connection);
    cmd.CommandText = readSql;
    cmd.Parameters.AddWithValue("@namespace", _namespace);
    cmd.Parameters.AddWithValue("@key_like", _keyLikeExpression?.Pattern);

    await using var reader = await cmd.ExecuteReaderAsync(_ct);
    if (await reader.ReadAsync(_ct))
    {
      var count = reader.GetInt32(0);
      return count;
    }

    return 0;
  }

  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  public async Task<int> CountSimpleDocuments<T>(LikeExpr? _keyLikeExpression, CancellationToken _ct)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return await Count(ns, _keyLikeExpression, _ct);
  }

  private long GetLatestDocumentId()
  {
    var counter = -1L;

    using (var cmd = new SQLiteCommand(p_connection))
    {
      cmd.CommandText =
          $"SELECT MAX(doc_id) " +
          $"FROM document_data; ";

      try
      {
        var max = (long)cmd.ExecuteScalar();

        counter = Math.Max(counter, max);
      }
      catch { }
    }

    return counter;
  }

}
