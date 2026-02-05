using Ax.Fw.Cache;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Data.Retention;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ax.Fw.Storage;

public class SqliteDocumentStorageV2 : DisposableStack, IDocumentStorage
{
  record CacheKey(string Namespace, string Key);

  private readonly SqliteBlobStorage p_blobStorage;
  private readonly JsonSerializerContext p_jsonCtx;
  private readonly SyncCache<CacheKey, object>? p_cache;

  /// <summary>
  /// Opens existing database or creates new
  /// </summary>
  /// <param name="_dbFilePath">Path to database file</param>
  /// <param name="_jsonCtx">Serialization context that will be used for internal (de-)serialization</param>
  public SqliteDocumentStorageV2(
    string _dbFilePath,
    JsonSerializerContext _jsonCtx,
    StorageCacheOptions? _cacheOptions = null,
    StorageRetentionOptions? _retentionOptions = null)
  {
    ArgumentNullException.ThrowIfNull(_jsonCtx, nameof(_jsonCtx));

    p_jsonCtx = _jsonCtx;
    p_blobStorage = ToDisposeOnEnded(new SqliteBlobStorage(_dbFilePath, _retentionOptions));

    if (_cacheOptions != null)
    {
      var cacheMaxValues = _cacheOptions.CacheCapacity - _cacheOptions.CacheCapacity / 10;
      var cacheOverhead = _cacheOptions.CacheCapacity / 10;
      p_cache = new SyncCache<CacheKey, object>(new SyncCacheSettings(cacheMaxValues, cacheOverhead, _cacheOptions.CacheTTL));
    }

    FilePath = _dbFilePath;
  }

  public string FilePath { get; }

  private BlobEntry<T> WriteDocumentInternal<T>(
    string _namespace,
    string _key,
    T _data)
  {
    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(_data, typeof(T), p_jsonCtx);
    var meta = p_blobStorage.WriteBlob(_namespace, _key, jsonBytes);
    return new BlobEntry<T>(meta.DocId, meta.Namespace, meta.Key, meta.LastModified, meta.Created, meta.Version, meta.RawLength, _data);
  }

  /// <summary>
  /// Upsert document to database
  /// </summary>
  public BlobEntry<T> WriteDocument<T>(
    string _namespace,
    KeyAlike _key,
    T _data) where T : notnull
  {
    var document = WriteDocumentInternal(_namespace, _key.Key, _data);
    p_cache?.Put(new CacheKey(_namespace, _key.Key), document);
    return document;
  }

  /// <summary>
  /// Upsert document to database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public BlobEntry<T> WriteSimpleDocument<T>(
    KeyAlike _key,
    T _data) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return WriteDocument(ns, _key, _data);
  }

  /// <summary>
  /// Delete document from the database
  /// </summary>
  public void DeleteDocuments(
    string _namespace,
    KeyAlike? _key,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    try
    {
      p_blobStorage.DeleteBlobs(_namespace, _key, _from, _to);
    }
    finally
    {
      RemoveEntriesFromCache(_namespace, _key?.Key);
    }
  }

  /// <summary>
  /// Delete document from the database
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public void DeleteSimpleDocument<T>(
    KeyAlike _key) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    DeleteDocuments(ns, _key, null, null);
  }

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  public IEnumerable<BlobEntryMeta> ListDocumentsMeta(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    return p_blobStorage
      .ListBlobsMeta(_namespace, _keyLikeExpression, _from, _to);
  }

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  public IEnumerable<BlobEntryMeta> ListDocumentsMeta(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    return p_blobStorage
      .ListBlobsMeta(_namespaceLikeExpression, _keyLikeExpression, _from, _to);
  }

  /// <summary>
  /// List documents
  /// </summary>
  public IEnumerable<BlobEntry<T>> ListDocuments<T>(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    return p_blobStorage.ListBlobs(_namespace, (_s, _meta) =>
    {
      var data = (T?)JsonSerializer.Deserialize(_s, typeof(T), p_jsonCtx)
        ?? throw new FormatException($"Data of document '{_meta.DocId}' is malformed!");

      return data;
    }, _keyLikeExpression, _from, _to);
  }

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public IEnumerable<BlobEntry<T>> ListSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ListDocuments<T>(ns, _keyLikeExpression, _from, _to);
  }

  private BlobEntry<T>? ReadDocumentInternal<T>(
    string _namespace,
    string _key)
  {
    if (!p_blobStorage.TryReadBlob(_namespace, _key, out BlobStream? blobBytes, out var meta))
      return null;

    try
    {
      var jsonTypeInfo = p_jsonCtx.GetTypeInfo(typeof(T))
        ?? throw new InvalidOperationException($"Json type info for type '{typeof(T)}' is not found in the provided JsonSerializerContext!");

      var obj = (T?)JsonSerializer.Deserialize(blobBytes, jsonTypeInfo)
        ?? throw new FormatException($"Data of document '{meta.DocId}' is malformed!");

      return new BlobEntry<T>(meta.DocId, meta.Namespace, meta.Key, meta.LastModified, meta.Created, meta.Version, meta.RawLength, obj);
    }
    finally
    {
      blobBytes.Dispose();
    }
  }

  /// <summary>
  /// Read document from the database
  /// </summary>
  public BlobEntry<T>? ReadDocument<T>(
    string _namespace,
    KeyAlike _key)
  {
    var key = _key.Key;
    if (p_cache?.TryGet(new CacheKey(_namespace, key), out var cachedValue) == true)
      return cachedValue as BlobEntry<T>;

    var result = ReadDocumentInternal<T>(_namespace, key);

    p_cache?.Put(new CacheKey(_namespace, key), result);

    return result;
  }

  /// <summary>
  /// Read document from the database and deserialize data
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  public BlobEntry<T>? ReadSimpleDocument<T>(
    KeyAlike _key) where T : notnull
  {
    var ns = typeof(T).GetNamespaceFromType();
    return ReadDocument<T>(ns, _key);
  }

  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  public void CompactDatabase()
    => p_blobStorage.CompactDatabase();

  /// <summary>
  /// Flushes temporary file to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  public void Flush(bool _force)
    => p_blobStorage.Flush(_force);

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  public long Count(
    string? _namespace,
    LikeExpr? _keyLikeExpression)
    => p_blobStorage.Count(_namespace, _keyLikeExpression);

  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  public long CountSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return Count(ns, _keyLikeExpression);
  }

  private void RemoveEntriesFromCache(string _namespace, string? _key)
  {
    if (p_cache == null)
      return;

    if (_key != null)
    {
      var cacheKey = new CacheKey(_namespace, _key);
      p_cache.TryRemove(cacheKey, out _);
      return;
    }

    foreach (var cacheEntry in p_cache.GetValues())
    {
      var cacheKey = cacheEntry.Key;
      var cacheValue = cacheEntry.Value;
      if (cacheValue == null)
        continue;

      if (_namespace == cacheKey.Namespace)
        p_cache.TryRemove(cacheKey, out _);
    }
  }

}
