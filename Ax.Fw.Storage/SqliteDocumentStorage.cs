﻿using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Storage.Attributes;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Globalization;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage;

public class SqliteDocumentStorage : IDocumentStorage, IDisposable
{
    private readonly SQLiteConnection p_connection;
    private readonly ConcurrentDictionary<Type, string> p_namespacePerType = new();
    private long p_documentsCounter = 0;
    private bool p_disposedValue;

    /// <summary>
    /// Opens existing database or creates new
    /// </summary>
    /// <param name="_dbFilePath">Path to database file</param>
    /// <param name="_lifetime">If lifetime is null you should dispose this instance of <see cref="SqliteDocumentStorage"/> by calling <see cref="Dispose"/> method</param>
    public SqliteDocumentStorage(string _dbFilePath, IReadOnlyLifetime? _lifetime = null)
    {
        p_connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;").OpenAndReturn();
        _lifetime?.DisposeOnCompleted(p_connection);

        using var command = new SQLiteCommand(p_connection);
        command.CommandText =
            $"PRAGMA synchronous = NORMAL; " +
            $"PRAGMA journal_mode = WAL; " +
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

    public async Task<DocumentEntry> WriteDocumentAsync(string _namespace, string _key, JToken _data, CancellationToken _ct)
    {
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
        command.Parameters.AddWithValue("@data", _data.ToString(Newtonsoft.Json.Formatting.None));

        await using var reader = await command.ExecuteReaderAsync(_ct);
        if (await reader.ReadAsync(_ct))
        {
            var docId = reader.GetInt32(0);
            var version = reader.GetInt64(1);
            var created = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
            return new DocumentEntry(docId, _namespace, _key, now, created, version, _data);
        }

        throw new InvalidOperationException($"Can't create document - db reader returned no result");
    }

    public DocumentEntry WriteDocument(string _namespace, string _key, JToken _data)
    {
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

        using var command = new SQLiteCommand(p_connection);
        command.CommandText = insertSql;
        command.Parameters.AddWithValue("@doc_id", Interlocked.Increment(ref p_documentsCounter));
        command.Parameters.AddWithValue("@namespace", _namespace);
        command.Parameters.AddWithValue("@key", _key);
        command.Parameters.AddWithValue("@last_modified", now.UtcTicks);
        command.Parameters.AddWithValue("@created", now.UtcTicks);
        command.Parameters.AddWithValue("@version", 1);
        command.Parameters.AddWithValue("@data", _data.ToString(Newtonsoft.Json.Formatting.None));

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var docId = reader.GetInt32(0);
            var version = reader.GetInt64(1);
            var created = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
            return new DocumentEntry(docId, _namespace, _key, now, created, version, _data);
        }

        throw new InvalidOperationException($"Can't create document - db reader returned no result");
    }

    public async Task<DocumentEntry> WriteDocumentAsync(string _namespace, int _key, JToken _data, CancellationToken _ct)
    {
        return await WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _data, _ct);
    }

    public async Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, string _key, T _data, CancellationToken _ct) where T : notnull
    {
        return await WriteDocumentAsync(_namespace, _key, JToken.FromObject(_data), _ct);
    }

    public async Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, int _key, T _data, CancellationToken _ct) where T : notnull
    {
        return await WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), JToken.FromObject(_data), _ct);
    }

    public DocumentEntry WriteDocument<T>(string _namespace, int _key, T _data) where T : notnull
    {
        return WriteDocument(_namespace, _key.ToString(CultureInfo.InvariantCulture), JToken.FromObject(_data));
    }

    /// <summary>
    /// Writes document to document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentEntry> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct) where T : notnull
    {
        var tableName = GetNamespaceFromType(typeof(T));

        return await WriteDocumentAsync(tableName, _entryId, JToken.FromObject(_data), _ct);
    }

    /// <summary>
    /// Creates new document or overwrites old
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentEntry> WriteSimpleDocumentAsync<T>(int _entryId, T _data, CancellationToken _ct) where T : notnull
    {
        return await WriteSimpleDocumentAsync(_entryId.ToString(CultureInfo.InvariantCulture), _data, _ct);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_namespace"></param>
    /// <param name="_key">Key of entry to delete. If <see cref="null"/> then delete all entries in namespace</param>
    /// <param name="_from"></param>
    /// <param name="_to"></param>
    /// <param name="_ct"></param>
    /// <returns></returns>
    public async Task DeleteDocumentsAsync(
        string _namespace,
        string? _key,
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        CancellationToken _ct)
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

    public async Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
    {
        var tableName = GetNamespaceFromType(typeof(T));

        await DeleteDocumentsAsync(tableName, _entryId, null, null, _ct);
    }

    public async Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
    {
        await DeleteSimpleDocumentAsync<T>(_entryId.ToString(CultureInfo.InvariantCulture), _ct);
    }

    public async IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(
        string _namespace,
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        [EnumeratorCancellation] CancellationToken _ct)
    {
        var listSql =
            $"SELECT doc_id, key, last_modified, created, version, data " +
            $"FROM document_data " +
            $"WHERE " +
            $"  @namespace=namespace AND " +
            $"  (@from IS NULL OR last_modified>=@from) AND " +
            $"  (@to IS NULL OR last_modified<=@to); ";

        //await CreateTableIfNeeded(normalizedTableName, _ct);

        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText = listSql;
        cmd.Parameters.AddWithValue("@namespace", _namespace);
        cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
        cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

        await using var reader = await cmd.ExecuteReaderAsync(_ct);
        while (await reader.ReadAsync(_ct))
        {
            var docId = reader.GetInt32(0);
            var optionalKey = reader.GetString(1);
            var lastModified = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
            var created = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
            var version = reader.GetInt64(4);
            var data = JToken.Parse(reader.GetString(5));

            yield return new DocumentEntry(docId, _namespace, optionalKey, lastModified, created, version, data);
        }
    }

    /// <summary>
    /// Retrieves the list of records from document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        [EnumeratorCancellation] CancellationToken _ct)
    {
        var tableName = GetNamespaceFromType(typeof(T));

        await foreach (var document in ListDocumentsAsync(tableName, _from, _to, _ct))
        {
            var data = document.Data.ToObject<T>();
            if (data == null)
                continue;

            var typedDocument = new DocumentTypedEntry<T>(
                document.DocId,
                document.Namespace,
                document.Key,
                document.LastModified,
                document.Created,
                document.Version,
                data);

            yield return typedDocument;
        }
    }

    public async Task<DocumentEntry?> ReadDocumentAsync(
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

        //await CreateTableIfNeeded(normalizedTableName, _ct);

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
            var data = JToken.Parse(reader.GetString(5));

            return new DocumentEntry(docId, _namespace, optionalKey, lastModified, created, version, data);
        }

        return null;
    }

    public async Task<DocumentEntry?> ReadDocumentAsync(
        string _namespace,
        int _key,
        CancellationToken _ct)
    {
        return await ReadDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _ct);
    }

    public async Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(
        string _namespace,
        string _key,
        CancellationToken _ct)
    {
        var document = await ReadDocumentAsync(_namespace, _key, _ct);
        if (document == null)
            return null;

        var data = document.Data.ToObject<T>();
        if (data == null)
            return null;

        var typedDocument = new DocumentTypedEntry<T>(
            document.DocId,
            document.Namespace,
            document.Key,
            document.LastModified,
            document.Created,
            document.Version,
            data);

        return typedDocument;
    }

    public async Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(
        string _namespace,
        int _key,
        CancellationToken _ct)
    {
        return await ReadTypedDocumentAsync<T>(_namespace, _key.ToString(CultureInfo.InvariantCulture), _ct);
    }

    /// <summary>
    /// Reads document from document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
    {
        var tableName = GetNamespaceFromType(typeof(T));

        var document = await ReadDocumentAsync(tableName, _entryId, _ct);
        if (document == null)
            return null;

        var data = document.Data.ToObject<T>();
        if (data == null)
            return null;

        return new DocumentTypedEntry<T>(
            document.DocId,
            document.Namespace,
            document.Key,
            document.LastModified,
            document.Created,
            document.Version,
            data);
    }

    /// <summary>
    /// Reads document from document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
    {
        return await ReadSimpleDocumentAsync<T>(_entryId.ToString(), _ct);
    }

    public async Task CompactDatabase(CancellationToken _ct)
    {
        try
        {
            await using var command = new SQLiteCommand(p_connection);
            command.CommandText = "VACUUM;";
            await command.ExecuteNonQueryAsync(_ct);
        }
        catch { }
    }
        
    protected virtual void Dispose(bool _disposing)
    {
        if (!p_disposedValue)
        {
            if (_disposing)
                p_connection.Dispose();

            p_disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(_disposing: true);
        GC.SuppressFinalize(this);
    }

    private string GetNamespaceFromType(Type _type)
    {
        if (p_namespacePerType.TryGetValue(_type, out var ns))
            return ns;

        ns = _type.GetCustomAttribute<SimpleDocumentAttribute>()?.Namespace;

        if (ns == null)
        {
            var underlyingType = Nullable.GetUnderlyingType(_type);
            if (underlyingType != null)
                ns = $"autotype_nullable_{underlyingType.FullName?.ToLower() ?? underlyingType.Name.ToLower()}";
            else
                ns = $"autotype_{_type.FullName?.ToLower() ?? _type.Name.ToLower()}";
        }

        p_namespacePerType[_type] = ns;
        return ns;
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

        return counter + 1;
    }

}