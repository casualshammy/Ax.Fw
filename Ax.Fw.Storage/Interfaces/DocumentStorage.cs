using Ax.Fw.Storage.Data;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.Interfaces;

public abstract class DocumentStorage : DisposableStack, IDocumentStorage
{
  public abstract Task CompactDatabase(CancellationToken _ct);

  /// <summary>
  /// Flushes temporary file (WAL) to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  public abstract Task FlushAsync(bool _force, CancellationToken _ct);

  public abstract Task<int> Count(string? _namespace, CancellationToken _ct);
  public abstract Task<int> CountSimpleDocuments<T>(CancellationToken _ct);
  public abstract Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from = null, DateTimeOffset? _to = null, CancellationToken _ct = default);
  public abstract Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull;
  public abstract Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull;
#pragma warning disable CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable

  /// <summary>
  /// List documents
  /// </summary>
  /// /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  public abstract IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null, 
    DateTimeOffset? _to = null, 
    [EnumeratorCancellation] CancellationToken _ct = default);

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  public abstract IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(
    string? _namespace, 
    LikeExpr? _keyLikeExpression = null, 
    DateTimeOffset? _from = null, 
    DateTimeOffset? _to = null, 
    [EnumeratorCancellation] CancellationToken _ct = default);

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  public abstract IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null, 
    DateTimeOffset? _to = null, 
    [EnumeratorCancellation] CancellationToken _ct = default);

#pragma warning restore CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
  public abstract Task<DocumentEntry?> ReadDocumentAsync(string _namespace, string _key, CancellationToken _ct);
  public abstract Task<DocumentEntry?> ReadDocumentAsync(string _namespace, int _key, CancellationToken _ct);
  public abstract Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull;
  public abstract Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull;
  public abstract Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, string _key, CancellationToken _ct);
  public abstract Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, int _key, CancellationToken _ct);
  public abstract Task<DocumentEntry> WriteDocumentAsync(string _namespace, string _key, JToken _data, CancellationToken _ct);
  public abstract Task<DocumentEntry> WriteDocumentAsync(string _namespace, int _key, JToken _data, CancellationToken _ct);
  public abstract Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, string _key, T _data, CancellationToken _ct) where T : notnull;
  public abstract Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, int _key, T _data, CancellationToken _ct) where T : notnull;
  public abstract Task<DocumentEntry> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct) where T : notnull;
  public abstract Task<DocumentEntry> WriteSimpleDocumentAsync<T>(int _entryId, T _data, CancellationToken _ct) where T : notnull;
}
