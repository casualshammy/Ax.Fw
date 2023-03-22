using Ax.Fw.Storage.Data;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.Interfaces;

public interface IDocumentStorage
{
  Task CompactDatabase(CancellationToken _ct);
  Task<int> Count(string? _namespace, CancellationToken _ct);
  Task<int> CountSimpleDocuments<T>(CancellationToken _ct);
  Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from = null, DateTimeOffset? _to = null, CancellationToken _ct = default);
  Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull;
  Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull;
#pragma warning disable CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
  IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(string _namespace, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(string? _namespace, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);
  IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);
#pragma warning restore CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
  Task<DocumentEntry?> ReadDocumentAsync(string _namespace, string _key, CancellationToken _ct);
  Task<DocumentEntry?> ReadDocumentAsync(string _namespace, int _key, CancellationToken _ct);
  Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull;
  Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull;
  Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, string _key, CancellationToken _ct);
  Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, int _key, CancellationToken _ct);
  Task<DocumentEntry> WriteDocumentAsync(string _namespace, string _key, JToken _data, CancellationToken _ct);
  Task<DocumentEntry> WriteDocumentAsync(string _namespace, int _key, JToken _data, CancellationToken _ct);
  Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, string _key, T _data, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, int _key, T _data, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct) where T : notnull;
  Task<DocumentEntry> WriteSimpleDocumentAsync<T>(int _entryId, T _data, CancellationToken _ct) where T : notnull;
}