using Ax.Fw.Cache;
using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using Newtonsoft.Json.Linq;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.StorageTypes;

public class CachedSqliteDocumentStorage : DocumentStorage
{
  record CacheKey(string Namespace, string Key);

  private readonly DocumentStorage p_documentStorage;
  private readonly SyncCache<CacheKey, DocumentEntry> p_cache;

  internal CachedSqliteDocumentStorage(
    DocumentStorage _documentStorage,
    int _cacheCapacity,
    int _cacheOverhead,
    TimeSpan _cacheTtl)
  {
    p_documentStorage = ToDispose(_documentStorage);
    p_cache = new SyncCache<CacheKey, DocumentEntry>(new SyncCacheSettings(_cacheCapacity, _cacheOverhead, _cacheTtl));

    if (_documentStorage is DocumentStorageWithRetentionRules docStorageWithRetention)
    {
      ToDispose(docStorageWithRetention.DeletedDocsFlow.
        Subscribe(_docs =>
        {
          foreach (var doc in _docs)
            RemoveEntriesFromCache(doc.Namespace, doc.Key);
        }));
    }
  }

  public override Task CompactDatabase(CancellationToken _ct) => p_documentStorage.CompactDatabase(_ct);

  /// <summary>
  /// Flushes temporary file to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  public override Task FlushAsync(bool _force, CancellationToken _ct) => p_documentStorage.FlushAsync(_force, _ct);

  public override Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from = null, DateTimeOffset? _to = null, CancellationToken _ct = default)
  {
    RemoveEntriesFromCache(_namespace, _key);

    return p_documentStorage.DeleteDocumentsAsync(_namespace, _key, _from, _to, _ct);
  }

  public override Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return DeleteDocumentsAsync(ns, _entryId, null, null, _ct);
  }

  public override Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct)
  {
    return DeleteSimpleDocumentAsync<T>(_entryId.ToString(CultureInfo.InvariantCulture), _ct);
  }

#pragma warning disable CS8424
  public override IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(
    string _namespace, 
    LikeExpr? _keyLikeExpression = null, 
    DateTimeOffset? _from = null, 
    DateTimeOffset? _to = null, 
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    return p_documentStorage.ListDocumentsAsync(_namespace, _keyLikeExpression, _from, _to, _ct);
  }

  public override IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null, 
    DateTimeOffset? _from = null, 
    DateTimeOffset? _to = null, 
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    return p_documentStorage.ListDocumentsMetaAsync(_namespace, _keyLikeExpression, _from, _to, _ct);
  }

  public override IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(
    LikeExpr? _keyLikeExpression = null, 
    DateTimeOffset? _from = null, 
    DateTimeOffset? _to = null, 
    [EnumeratorCancellation] CancellationToken _ct = default)
  {
    return p_documentStorage.ListSimpleDocumentsAsync<T>(_keyLikeExpression, _from, _to, _ct);
  }
#pragma warning restore CS8424

  public override Task<DocumentEntry?> ReadDocumentAsync(string _namespace, string _key, CancellationToken _ct)
  {
    var cacheKey = new CacheKey(_namespace, _key);
    var result = p_cache.GetOrPutAsync(cacheKey, _ => p_documentStorage.ReadDocumentAsync(_namespace, _key, _ct));
    return result;
  }

  public override Task<DocumentEntry?> ReadDocumentAsync(string _namespace, int _key, CancellationToken _ct)
  {
    return ReadDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _ct);
  }

  public override Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ReadTypedDocumentAsync<T>(ns, _entryId, _ct);
  }

  public override Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct)
  {
    return ReadSimpleDocumentAsync<T>(_entryId.ToString(CultureInfo.InvariantCulture), _ct);
  }

  public override async Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, string _key, CancellationToken _ct)
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

  public override Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, int _key, CancellationToken _ct)
  {
    return ReadTypedDocumentAsync<T>(_namespace, _key.ToString(CultureInfo.InvariantCulture), _ct);
  }

  public override async Task<DocumentEntry> WriteDocumentAsync(string _namespace, string _key, JToken _data, CancellationToken _ct)
  {
    var document = await p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);
    p_cache.Put(new CacheKey(_namespace, _key), document);
    return document;
  }

  public override Task<DocumentEntry> WriteDocumentAsync(string _namespace, int _key, JToken _data, CancellationToken _ct)
  {
    return WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _data, _ct);
  }

  public override Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, string _key, T _data, CancellationToken _ct)
  {
    var jToken = JToken.FromObject(_data);
    return WriteDocumentAsync(_namespace, _key, jToken, _ct);
  }

  public override Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, int _key, T _data, CancellationToken _ct)
  {
    return WriteDocumentAsync(_namespace, _key.ToString(CultureInfo.InvariantCulture), _data, _ct);
  }

  public override Task<DocumentEntry> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return WriteDocumentAsync(ns, _entryId, _data, _ct);
  }

  public override Task<DocumentEntry> WriteSimpleDocumentAsync<T>(int _entryId, T _data, CancellationToken _ct)
  {
    return WriteSimpleDocumentAsync(_entryId.ToString(CultureInfo.InvariantCulture), _data, _ct);
  }

  public override Task<int> Count(string? _namespace, CancellationToken _ct) => p_documentStorage.Count(_namespace, _ct);

  public override Task<int> CountSimpleDocuments<T>(CancellationToken _ct) => p_documentStorage.CountSimpleDocuments<T>(_ct);

  private void RemoveEntriesFromCache(string _namespace, string? _key)
  {
    foreach (var cacheEntry in p_cache.GetValues())
    {
      var cacheKey = cacheEntry.Key;
      var cacheValue = cacheEntry.Value;
      if (cacheValue == null)
        continue;

      if (_namespace == cacheKey.Namespace && (_key == null || _key == cacheKey.Key))
        p_cache.TryRemove(cacheKey, out _);
    }
  }

}
