using Ax.Fw.Storage.Data;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace Ax.Fw.Storage.Interfaces;

public interface IDocumentStorageAot
{
  Task CompactDatabase(CancellationToken _ct);
  Task<int> Count(string? _namespace, LikeExpr? _keyLikeExpression, CancellationToken _ct);
  Task<int> CountSimpleDocuments<T>(LikeExpr? _keyLikeExpression, CancellationToken _ct);
  Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from = null, DateTimeOffset? _to = null, CancellationToken _ct = default);
  Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull;
  Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull;
  Task FlushAsync(bool _force, CancellationToken _ct);
  IAsyncEnumerable<DocumentEntry<T>> ListDocumentsAsync<T>(string _namespace, JsonTypeInfo<T> _jsonTypeInfo, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);
  IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(string? _namespace, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);
  IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(LikeExpr? _namespaceLikeExpression = null, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);
  IAsyncEnumerable<DocumentEntry<T>> ListSimpleDocumentsAsync<T>(JsonTypeInfo<T> _jsonTypeInfo, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);
  Task<DocumentEntry<T>?> ReadDocumentAsync<T>(string _namespace, string _key, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct);
  Task<DocumentEntry<T>?> ReadDocumentAsync<T>(string _namespace, int _key, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct);
  Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry<T>> WriteDocumentAsync<T>(string _namespace, string _key, T _data, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry<T>> WriteDocumentAsync<T>(string _namespace, int _key, T _data, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(string _entryId, T _data, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry<T>> WriteSimpleDocumentAsync<T>(int _entryId, T _data, JsonTypeInfo<T> _jsonTypeInfo, CancellationToken _ct) where T : notnull;
}