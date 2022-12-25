using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Storage.Attributes;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage;

public class SqliteDocumentStorage : IDocumentStorage
{
    private readonly SQLiteConnection p_connection;
    private readonly ConcurrentDictionary<Type, string> p_tableNamesPerType = new();
    private readonly HashSet<string> p_existingTables = new();
    private long p_documentsCounter = 0;

    public SqliteDocumentStorage(string _dbFilePath, IReadOnlyLifetime _lifetime)
    {
        p_connection = _lifetime.DisposeOnCompleted(GetConnection(_dbFilePath, _lifetime));
        p_documentsCounter = GetLatestDocumentId(p_connection);
    }

    public async Task<DocumentEntry> WriteDocumentAsync(string _namespace, string _key, JToken _data, CancellationToken _ct)
    {
        var normalizedTableName = GetNormalizedTableName(_namespace);
        var now = DateTimeOffset.UtcNow;

        using var inject = InjectTableCreation(normalizedTableName,
            $"INSERT OR REPLACE INTO [{normalizedTableName}] (doc_id, key, last_modified, created, version, data) " +
            $"VALUES (@doc_id, @key, @last_modified, @created, @version, @data) " +
            $"ON CONFLICT (key) " +
            $"DO UPDATE SET " +
            $"  last_modified=@last_modified, " +
            $"  version=version+1, " +
            $"  data=@data " +
            $"RETURNING doc_id, version, created; ", out var sql);

        await using var command = new SQLiteCommand(p_connection);
        command.CommandText = sql;
        command.Parameters.AddWithValue("@doc_id", Interlocked.Increment(ref p_documentsCounter));
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

    /// <summary>
    /// Writes document to document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentEntry> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct) where T : notnull
    {
        var tableName = GetTableNameFromType(typeof(T));

        return await WriteDocumentAsync(tableName, _entryId, JToken.FromObject(_data), _ct);
    }

    /// <summary>
    /// Creates new document or overwrites old
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
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
        var normalizedTableName = GetNormalizedTableName(_namespace);

        using var inject = InjectTableCreation(normalizedTableName,
                        $"DELETE FROM [{normalizedTableName}] " +
                        $"WHERE " +
                        $"  (@key IS NULL OR @key=key) AND " +
                        $"  (@from IS NULL OR last_modified>=@from) AND " +
                        $"  (@to IS NULL OR last_modified<=@to); ", out var sql);

        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@key", _key);
        cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
        cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

        await cmd.ExecuteNonQueryAsync(_ct);
    }

    public async Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
    {
        var tableName = GetTableNameFromType(typeof(T));

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
        var normalizedTableName = GetNormalizedTableName(_namespace);

        using var inject = InjectTableCreation(normalizedTableName,
            $"SELECT doc_id, key, last_modified, created, version, data " +
            $"FROM [{normalizedTableName}] " +
            $"WHERE " +
            $"  (@from IS NULL OR last_modified>=@from) AND " +
            $"  (@to IS NULL OR last_modified<=@to); ", out var sql);

        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText = sql;
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
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        [EnumeratorCancellation] CancellationToken _ct)
    {
        var tableName = GetTableNameFromType(typeof(T));

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
        var normalizedTableName = GetNormalizedTableName(_namespace);

        using var inject = InjectTableCreation(normalizedTableName,
            $"SELECT doc_id, key, last_modified, created, version, data " +
            $"FROM [{normalizedTableName}] " +
            $"WHERE " +
            $"  key=@key; ", out var sql);

        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText = sql;
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
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
    {
        var tableName = GetTableNameFromType(typeof(T));

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
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
    {
        return await ReadSimpleDocumentAsync<T>(_entryId.ToString(), _ct);
    }

    private string GetTableNameFromType(Type _type)
    {
        if (p_tableNamesPerType.TryGetValue(_type, out var tableName))
            return tableName;

        tableName = _type.GetCustomAttribute<SimpleRecordAttribute>()?.TableName;

        if (tableName == null)
        {
            var underlyingType = Nullable.GetUnderlyingType(_type);
            if (underlyingType != null)
                tableName = $"autotype_nullable_{underlyingType.FullName?.ToLower() ?? underlyingType.Name.ToLower()}";
            else
                tableName = $"autotype_{_type.FullName?.ToLower() ?? _type.Name.ToLower()}";
        }

        tableName = tableName.Replace('.', '_').Replace("-", "_");

        p_tableNamesPerType[_type] = tableName;
        return tableName;
    }

    private IDisposable InjectTableCreation(string _tableId, string _sql, out string _resultSql)
    {
        if (!p_existingTables.Contains(_tableId))
        {
            _resultSql =
                $"CREATE TABLE IF NOT EXISTS '{_tableId}' " +
                $"( " +
                $"  doc_id INTEGER PRIMARY KEY, " +
                $"  key TEXT NOT NULL UNIQUE, " +
                $"  last_modified INTEGER NOT NULL, " +
                $"  created INTEGER NOT NULL, " +
                $"  version INTEGER NOT NULL, " +
                $"  data TEXT NOT NULL " +
                $"); " + _sql;

            return Disposable.Create(() => p_existingTables.Add(_tableId));
        }

        _resultSql = _sql;
        return Disposable.Empty;
    }

    private static string GetNormalizedTableName(string _tableName) => $"afs_{_tableName}".Replace('.', '_').Replace("-", "_");

    private static SQLiteConnection GetConnection(string _dbFilePath, IReadOnlyLifetime _lifetime)
    {
        var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;").OpenAndReturn();
        Observable
            .Timer(TimeSpan.FromHours(1))
            .StartWith(0)
            .Delay(TimeSpan.FromMinutes(10))
            .Subscribe(_ =>
            {
                try
                {
                    using var command = new SQLiteCommand(connection);
                    command.CommandText = "VACUUM;";
                    command.ExecuteNonQuery();
                }
                catch { }
            }, _lifetime.Token);

        return connection;
    }

    private static long GetLatestDocumentId(SQLiteConnection _connection)
    {
        var tables = new HashSet<string>();

        using (var cmdTableNames = new SQLiteCommand(_connection))
        {
            cmdTableNames.CommandText =
                $"SELECT name " +
                $"FROM sqlite_schema " +
                $"WHERE " +
                $"  type ='table' AND " +
                $"  name LIKE 'afs_%';";

            using var reader = cmdTableNames.ExecuteReader();
            while (reader.Read())
            {
                var tableName = reader.GetString(0);
                tables.Add(tableName);
            }
        }

        var counter = -1L;

        foreach (var tableName in tables)
        {
            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText =
                    $"SELECT MAX(doc_id) " +
                    $"FROM @table_name; ";

                cmd.Parameters.AddWithValue("@table_name", tableName);

                try
                {
                    var max = (long)cmd.ExecuteScalar();

                    counter = Math.Max(counter, max);
                }
                catch { }
            }
        }

        return counter + 1;
    }

}