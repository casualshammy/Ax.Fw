using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Storage.Attributes;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage;

public class SqliteDocumentStorage : IDocumentStorage
{
    private const string TABLE_DOCUMENT_META = "document_meta";
    private const string TABLE_DOCUMENT_DATA = "document_data";

    private readonly SQLiteConnection p_connection;
    private readonly ConcurrentDictionary<Type, string> p_tableNamesPerType = new();

    public SqliteDocumentStorage(string _dbFilePath, IReadOnlyLifetime _lifetime)
    {
        p_connection = _lifetime.DisposeOnCompleted(GetConnection(_dbFilePath, _lifetime));
        Migrate();
    }

    public async Task<DocumentInfo> CreateDocumentAsync(string _docType, string? _namespace, CancellationToken _ct)
    {
        var now = DateTimeOffset.UtcNow;
        var ns = _namespace ?? "default";

        await using var command = new SQLiteCommand(p_connection);
        command.CommandText =
                $"INSERT OR REPLACE INTO {TABLE_DOCUMENT_META} (doc_type, namespace, version, last_modified) " +
                $"VALUES (@doc_type, @namespace, @version, @last_modified) " +
                $"RETURNING doc_id; ";
        command.Parameters.AddWithValue("@doc_type", _docType);
        command.Parameters.AddWithValue("@namespace", ns);
        command.Parameters.AddWithValue("@version", 0L);
        command.Parameters.AddWithValue("@last_modified", now.UtcTicks);

        await using var reader = await command.ExecuteReaderAsync(_ct);
        if (await reader.ReadAsync(_ct))
            return new DocumentInfo(reader.GetInt32(0), _docType, ns, 0L, now);

        throw new InvalidOperationException($"Can't create document - db reader returned no result");
    }

    public async Task DeleteDocumentAsync(int _docId, CancellationToken _ct)
    {
        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
                        $"DELETE FROM {TABLE_DOCUMENT_META} " +
                        $"WHERE doc_id=@doc_id; ";
        cmd.Parameters.AddWithValue("@doc_id", _docId);

        await cmd.ExecuteNonQueryAsync(_ct);
    }

    public async IAsyncEnumerable<DocumentInfo> ListDocumentsAsync(
        string? _docType,
        string? _namespace,
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        [EnumeratorCancellation] CancellationToken _ct)
    {
        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
            $"SELECT doc_id, doc_type, namespace, version, last_modified " +
            $"FROM {TABLE_DOCUMENT_META} " +
            $"WHERE " +
            $"  (@doc_type IS NULL OR doc_type=@doc_type) AND " +
            $"  (@namespace IS NULL OR namespace=@namespace) AND " +
            $"  (@from IS NULL OR last_modified>=@from) AND " +
            $"  (@to IS NULL OR last_modified<=@to); ";
        cmd.Parameters.AddWithValue("@doc_type", _docType);
        cmd.Parameters.AddWithValue("@namespace", _namespace);
        cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
        cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

        await using var reader = await cmd.ExecuteReaderAsync(_ct);
        while (await reader.ReadAsync(_ct))
        {
            var docId = reader.GetInt32(0);
            var docType = reader.GetString(1);
            var @namespace = reader.GetString(2);
            var version = reader.GetInt64(3);
            var lastModified = new DateTimeOffset(reader.GetInt64(4), TimeSpan.Zero);

            yield return new DocumentInfo(docId, docType, @namespace, version, lastModified);
        }
    }

    public async Task<DocumentInfo?> GetDocumentAsync(int _docId, CancellationToken _ct)
    {
        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
            $"SELECT doc_type, namespace, version, last_modified " +
            $"FROM {TABLE_DOCUMENT_META} " +
            $"WHERE " +
            $"  doc_id=@doc_id ";
        cmd.Parameters.AddWithValue("@doc_id", _docId);

        await using var reader = await cmd.ExecuteReaderAsync(_ct);
        if (await reader.ReadAsync(_ct))
        {
            var docType = reader.GetString(0);
            var @namespace = reader.GetString(1);
            var version = reader.GetInt64(2);
            var lastModified = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);

            return new DocumentInfo(_docId, docType, @namespace, version, lastModified);
        }

        return null;
    }

    public async Task<DocumentRecord> WriteRecordAsync(int _docId, string _tableId, string? _recordKey, JToken _data, CancellationToken _ct)
    {
        if (_recordKey == "")
            throw new ArgumentOutOfRangeException(nameof(_recordKey), "Record key must be null or non-empty string!");

        var now = DateTimeOffset.UtcNow;

        await using var command = new SQLiteCommand(p_connection);
        command.CommandText =
                $"UPDATE {TABLE_DOCUMENT_META} " +
                $"SET " +
                $"  last_modified=@last_modified, " +
                $"  version=version+1 " +
                $"WHERE doc_id=@doc_id; " +
                $"INSERT OR REPLACE INTO {TABLE_DOCUMENT_DATA} (doc_id, table_id, record_key, last_modified, data) " +
                $"VALUES (@doc_id, @table_id, @record_key, @last_modified, @data) " +
                $"ON CONFLICT (doc_id, table_id, record_key) " +
                $"DO UPDATE SET " +
                $"  last_modified=@last_modified, " +
                $"  data=@data " +
                $"RETURNING record_id; ";
        command.Parameters.AddWithValue("@doc_id", _docId);
        command.Parameters.AddWithValue("@table_id", _tableId);
        command.Parameters.AddWithValue("@record_key", _recordKey ?? "");
        command.Parameters.AddWithValue("@last_modified", now.UtcTicks);
        command.Parameters.AddWithValue("@data", _data.ToString(Newtonsoft.Json.Formatting.None));

        await using var reader = await command.ExecuteReaderAsync(_ct);
        if (await reader.ReadAsync(_ct))
            return new DocumentRecord(reader.GetInt32(0), _docId, _tableId, _recordKey, now, _data);

        throw new InvalidOperationException($"Can't create record - db reader returned no result");
    }

    /// <summary>
    /// Writes record to document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentRecord> WriteSimpleRecordAsync<T>(int _docId, T _data, CancellationToken _ct) where T : notnull
    {
        var tableName = GetTableNameFromType(typeof(T));

        return await WriteRecordAsync(_docId, tableName, null, JToken.FromObject(_data), _ct);
    }

    public async Task DeleteRecordsAsync(
        int _docId,
        string? _tableId,
        string? _recordKey,
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        CancellationToken _ct)
    {
        if (_recordKey == "")
            throw new ArgumentOutOfRangeException(nameof(_recordKey), "Record key must be null or non-empty string!");

        var now = DateTimeOffset.UtcNow;

        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
                        $"DELETE FROM {TABLE_DOCUMENT_DATA} " +
                        $"WHERE " +
                        $"  doc_id=@doc_id AND " +
                        $"  (@table_id IS NULL OR @table_id=table_id) AND " +
                        $"  (@record_key IS NULL OR @record_key=record_key) AND " +
                        $"  (@from IS NULL OR last_modified>=@from) AND " +
                        $"  (@to IS NULL OR last_modified<=@to); " +
                        $"UPDATE {TABLE_DOCUMENT_META} " +
                        $"SET " +
                        $"  last_modified=@last_modified, " +
                        $"  version=version+1 " +
                        $"WHERE doc_id=@doc_id; ";
        cmd.Parameters.AddWithValue("@doc_id", _docId);
        cmd.Parameters.AddWithValue("@table_id", _tableId);
        cmd.Parameters.AddWithValue("@record_key", _recordKey);
        cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
        cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);
        cmd.Parameters.AddWithValue("@last_modified", now.UtcTicks);

        await cmd.ExecuteNonQueryAsync(_ct);
    }

    public async Task DeleteSimpleRecordAsync<T>(int _docId, CancellationToken _ct) where T : notnull
    {
        var tableName = GetTableNameFromType(typeof(T));

        await DeleteRecordsAsync(_docId, tableName, null, null, null, _ct);
    }

    public async IAsyncEnumerable<DocumentRecord> ListRecordsAsync(
        int? _docId,
        string? _tableId,
        string? _recordKey,
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        [EnumeratorCancellation] CancellationToken _ct)
    {
        if (_recordKey == "")
            throw new ArgumentOutOfRangeException(nameof(_recordKey), "Record key must be null or non-empty string!");

        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
            $"SELECT record_id, doc_id, table_id, record_key, last_modified, data " +
            $"FROM {TABLE_DOCUMENT_DATA} " +
            $"WHERE " +
            $"  (@doc_id IS NULL OR doc_id=@doc_id) AND " +
            $"  (@table_id IS NULL OR table_id=@table_id) AND " +
            $"  (@record_key IS NULL OR record_key=@record_key) AND " +
            $"  (@from IS NULL OR last_modified>=@from) AND " +
            $"  (@to IS NULL OR last_modified<=@to); ";
        cmd.Parameters.AddWithValue("@doc_id", _docId);
        cmd.Parameters.AddWithValue("@table_id", _tableId);
        cmd.Parameters.AddWithValue("@record_key", _recordKey);
        cmd.Parameters.AddWithValue("@from", _from?.UtcTicks);
        cmd.Parameters.AddWithValue("@to", _to?.UtcTicks);

        await using var reader = await cmd.ExecuteReaderAsync(_ct);
        while (await reader.ReadAsync(_ct))
        {
            var recordId = reader.GetInt32(0);
            var docId = reader.GetInt32(1);
            var tableId = reader.GetString(2);
            var recordKey = reader.GetString(3);
            var lastModified = new DateTimeOffset(reader.GetInt64(4), TimeSpan.Zero);
            var data = JToken.Parse(reader.GetString(5));

            yield return new DocumentRecord(recordId, docId, tableId, recordKey != "" ? recordKey : null, lastModified, data);
        }
    }

    /// <summary>
    /// Retrieves the list of records from document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async IAsyncEnumerable<DocumentTypedRecord<T>> ListSimpleRecordsAsync<T>(
        int? _docId,
        string? _tableId,
        string? _recordKey,
        DateTimeOffset? _from,
        DateTimeOffset? _to,
        [EnumeratorCancellation] CancellationToken _ct)
    {
        await foreach (var record in ListRecordsAsync(_docId, _tableId, _recordKey, _from, _to, _ct))
        {
            var data = record.Data.ToObject<T>();
            if (data == null)
                continue;

            var typedRecord = new DocumentTypedRecord<T>(
                record.RecordId,
                record.DocId,
                record.TableId,
                record.RecordKey,
                record.LastModified,
                data);

            yield return typedRecord;
        }
    }

    public async Task<DocumentRecord?> ReadRecordAsync(int _recordId, CancellationToken _ct)
    {
        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
            $"SELECT doc_id, table_id, record_key, last_modified, data " +
            $"FROM {TABLE_DOCUMENT_DATA} " +
            $"WHERE " +
            $"  record_id=@record_id; ";
        cmd.Parameters.AddWithValue("@record_id", _recordId);

        await using var reader = await cmd.ExecuteReaderAsync(_ct);
        if (await reader.ReadAsync(_ct))
        {
            var docId = reader.GetInt32(0);
            var tableId = reader.GetString(1);
            var recordKey = reader.GetString(2);
            var lastModified = new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero);
            var data = JToken.Parse(reader.GetString(4));

            return new DocumentRecord(_recordId, docId, tableId, recordKey != "" ? recordKey : null, lastModified, data);
        }

        return null;
    }

    public async Task<DocumentRecord?> ReadRecordAsync(
        int _docId,
        string _tableId,
        string? _recordKey,
        CancellationToken _ct)
    {
        await using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
            $"SELECT record_id, record_key, last_modified, data " +
            $"FROM {TABLE_DOCUMENT_DATA} " +
            $"WHERE " +
            $"  doc_id=@doc_id AND " +
            $"  table_id=@table_id AND " +
            $"  record_key=@record_key; " +
        cmd.Parameters.AddWithValue("@doc_id", _docId);
        cmd.Parameters.AddWithValue("@table_id", _tableId);
        cmd.Parameters.AddWithValue("@record_key", _recordKey ?? "");

        await using var reader = await cmd.ExecuteReaderAsync(_ct);
        if (await reader.ReadAsync(_ct))
        {
            var recordId = reader.GetInt32(0);
            var recordKey = reader.GetString(1);
            var lastModified = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero);
            var data = JToken.Parse(reader.GetString(3));

            return new DocumentRecord(recordId, _docId, _tableId, recordKey != "" ? recordKey : null, lastModified, data);
        }

        return null;
    }

    /// <summary>
    /// Reads record from document
    /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleRecordAttribute"/>, records are treated as strongly-typed, so T = int IS NOT EQUAL to T = long or T = int?</para>
    /// </summary>
    public async Task<DocumentTypedRecord<T>?> ReadSimpleRecordAsync<T>(int _docId, CancellationToken _ct) where T : notnull
    {
        var tableName = GetTableNameFromType(typeof(T));

        var record = await ReadRecordAsync(_docId, tableName, null, _ct);
        if (record == null)
            return null;

        var data = record.Data.ToObject<T>();
        if (data == null)
            return null;

        return new DocumentTypedRecord<T>(
            record.RecordId, 
            record.DocId, 
            record.TableId, 
            record.RecordKey, 
            record.LastModified, 
            data);
    }

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

    private void Migrate()
    {
        using var cmd = new SQLiteCommand(p_connection);
        cmd.CommandText =
                $"PRAGMA foreign_keys = ON; " +
                $"CREATE TABLE IF NOT EXISTS {TABLE_DOCUMENT_META} " +
                $"( " +
                $"  doc_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                $"  doc_type TEXT NOT NULL, " +
                $"  namespace TEXT NOT NULL, " +
                $"  version INTEGER NOT NULL, " +
                $"  last_modified INTEGER NOT NULL " +
                $"); " +
                $"CREATE TABLE IF NOT EXISTS {TABLE_DOCUMENT_DATA} " +
                $"( " +
                $"  record_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                $"  doc_id INTEGER NOT NULL REFERENCES {TABLE_DOCUMENT_META}(doc_id) ON DELETE CASCADE, " +
                $"  table_id TEXT NOT NULL, " +
                $"  record_key TEXT NOT NULL, " +
                $"  last_modified INTEGER NOT NULL, " +
                $"  data TEXT NOT NULL, " +
                $"  UNIQUE(doc_id, table_id, record_key) " +
                $"); ";
        cmd.ExecuteNonQuery();
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
                tableName = $"primitive-type-nullable-{underlyingType.FullName?.ToLower() ?? underlyingType.Name.ToLower()}";
            else
                tableName = $"primitive-type-{_type.FullName?.ToLower() ?? _type.Name.ToLower()}";
        }

        p_tableNamesPerType[_type] = tableName;
        return tableName;
    }

}