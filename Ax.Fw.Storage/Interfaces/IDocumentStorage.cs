using Ax.Fw.Storage.Data;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.Interfaces;

public interface IDocumentStorage
{
    Task<DocumentInfo> CreateDocumentAsync(string _docType, string? _namespace, CancellationToken _ct);
    Task DeleteDocumentAsync(int _docId, CancellationToken _ct);
    Task DeleteRecordsAsync(int _docId, string? _tableId, string? _recordKey, DateTimeOffset? _from, DateTimeOffset? _to, CancellationToken _ct);
    Task DeleteSimpleRecordAsync<T>(int _docId, CancellationToken _ct) where T : notnull;
    Task<DocumentInfo?> GetDocumentAsync(int _docId, CancellationToken _ct);
    Task<DocumentRecord?> ReadRecordAsync(int _docId, string _tableId, string? _recordKey, CancellationToken _ct);
    Task<DocumentRecord?> ReadRecordAsync(int _recordId, CancellationToken _ct);
    Task<DocumentTypedRecord<T>?> ReadSimpleRecordAsync<T>(int _docId, CancellationToken _ct) where T : notnull;
    IAsyncEnumerable<DocumentInfo> ListDocumentsAsync(string? _docType, string? _namespace, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct);
    IAsyncEnumerable<DocumentTypedRecord<T>> ListSimpleRecordsAsync<T>(int? _docId, string? _tableId, string? _recordKey, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct);
    IAsyncEnumerable<DocumentRecord> ListRecordsAsync(int? _docId, string? _tableId, string? _recordKey, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct);
    Task<DocumentRecord> WriteRecordAsync(int _docId, string _tableId, string? _recordKey, JToken _data, CancellationToken _ct);
    Task<DocumentRecord> WriteSimpleRecordAsync<T>(int _docId, T _data, CancellationToken _ct) where T : notnull;
}