﻿using Ax.Fw.Storage.Data;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.Interfaces;

public interface IDocumentStorage : IDisposable
{
  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  Task CompactDatabase(CancellationToken _ct);

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  Task<int> Count(string? _namespace, CancellationToken _ct);

  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  Task<int> CountSimpleDocuments<T>(CancellationToken _ct);

  Task DeleteDocumentsAsync(
    string _namespace, 
    string? _key, 
    DateTimeOffset? _from = null, 
    DateTimeOffset? _to = null, 
    CancellationToken _ct = default);

  Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull;
  Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull;

  /// <summary>
  /// Flushes temporary file (WAL) to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  Task FlushAsync(bool _force, CancellationToken _ct);
#pragma warning disable CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable

  /// <summary>
  /// List documents
  /// </summary>
  /// /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default);

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  /// <param name="_namespaceLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with namespace starting with "tel:123-456-")</param>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default);
  IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(string? _namespace, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default);
  IAsyncEnumerable<DocumentTypedEntry<T>> ListTypedDocumentsAsync<T>(string _namespace, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null, [EnumeratorCancellation] CancellationToken _ct = default);

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