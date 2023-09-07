using Ax.Fw.Extensions;
using Ax.Fw.Pools;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.StorageTypes;

public class DocumentStorageWithRetentionRules : DisposableStack, IDocumentStorage
{
  private readonly IDocumentStorage p_documentStorage;
  private readonly Subject<ImmutableHashSet<DocumentEntryMeta>> p_deletedDocsFlow;

  internal DocumentStorageWithRetentionRules(
    IDocumentStorage _documentStorage,
    TimeSpan? _documentMaxAgeFromCreation = null,
    TimeSpan? _documentMaxAgeFromLastChange = null,
    TimeSpan? _scanInterval = null,
    Action<ImmutableHashSet<DocumentEntryMeta>>? _onDocsDeleteCallback = null)
  {
    p_documentStorage = ToDispose(_documentStorage);
    p_deletedDocsFlow = ToDispose(new Subject<ImmutableHashSet<DocumentEntryMeta>>());

    ToDispose(SharedPool<EventLoopScheduler>.Get(out var scheduler));

    var subscription = Observable
      .Interval(_scanInterval ?? TimeSpan.FromMinutes(10), scheduler)
      .StartWithDefault()
      .ObserveOn(scheduler)
      .SelectAsync(async (_, _ct) =>
      {
        var now = DateTimeOffset.UtcNow;
        var docsToDeleteBuilder = ImmutableHashSet.CreateBuilder<DocumentEntryMeta>();

        await foreach (var doc in _documentStorage.ListDocumentsMetaAsync((string?)null, _ct: _ct).ConfigureAwait(false))
        {
          var docAge = now - doc.Created;
          var docLastModifiedAge = now - doc.LastModified;
          if (_documentMaxAgeFromCreation != null && docAge > _documentMaxAgeFromCreation)
            docsToDeleteBuilder.Add(doc);
          else if (_documentMaxAgeFromLastChange != null && docLastModifiedAge > _documentMaxAgeFromLastChange)
            docsToDeleteBuilder.Add(doc);
        }

        foreach (var doc in docsToDeleteBuilder)
          await _documentStorage.DeleteDocumentsAsync(doc.Namespace, doc.Key, null, null, _ct);

        try
        {
          var hashSet = docsToDeleteBuilder.ToImmutable();
          p_deletedDocsFlow.OnNext(hashSet);
          _onDocsDeleteCallback?.Invoke(hashSet);
        }
        catch { }
      }, scheduler)
      .Subscribe();

    ToDispose(subscription);
  }

  public IObservable<ImmutableHashSet<DocumentEntryMeta>> DeletedDocsFlow => p_deletedDocsFlow;

  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  public Task CompactDatabase(CancellationToken _ct) => p_documentStorage.CompactDatabase(_ct);

  /// <summary>
  /// Flushes temporary file to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  public Task FlushAsync(bool _force, CancellationToken _ct) => p_documentStorage.FlushAsync(_force, _ct);

  public Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from = null, DateTimeOffset? _to = null, CancellationToken _ct = default)
    => p_documentStorage.DeleteDocumentsAsync(_namespace, _key, _from, _to, _ct);

  public Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
    => p_documentStorage.DeleteSimpleDocumentAsync<T>(_entryId, _ct);

  public Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
    => p_documentStorage.DeleteSimpleDocumentAsync<T>(_entryId, _ct);

#pragma warning disable CS8424
  public IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default)
    => p_documentStorage.ListDocumentsAsync(_namespace, _keyLikeExpression, _from, _to, _ct);

  public IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default)
    => p_documentStorage.ListDocumentsMetaAsync(_namespaceLikeExpression, _keyLikeExpression, _from, _to, _ct);

  public IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default) 
    => p_documentStorage.ListDocumentsMetaAsync(_namespace, _keyLikeExpression, _from, _to, _ct);

  public IAsyncEnumerable<DocumentTypedEntry<T>> ListTypedDocumentsAsync<T>(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default) 
    => p_documentStorage.ListTypedDocumentsAsync<T>(_namespace, _keyLikeExpression, _from, _to, _ct);

  public IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null,
    [EnumeratorCancellation] CancellationToken _ct = default)
    => p_documentStorage.ListSimpleDocumentsAsync<T>(_keyLikeExpression, _from, _to, _ct);
#pragma warning restore CS8424

  public Task<DocumentEntry?> ReadDocumentAsync(string _namespace, string _key, CancellationToken _ct)
    => p_documentStorage.ReadDocumentAsync(_namespace, _key, _ct);

  public Task<DocumentEntry?> ReadDocumentAsync(string _namespace, int _key, CancellationToken _ct)
    => p_documentStorage.ReadDocumentAsync(_namespace, _key, _ct);

  public Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
    => p_documentStorage.ReadSimpleDocumentAsync<T>(_entryId, _ct);

  public Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
    => p_documentStorage.ReadSimpleDocumentAsync<T>(_entryId, _ct);

  public Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, string _key, CancellationToken _ct)
    => p_documentStorage.ReadTypedDocumentAsync<T>(_namespace, _key, _ct);

  public Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, int _key, CancellationToken _ct)
    => p_documentStorage.ReadTypedDocumentAsync<T>(_namespace, _key, _ct);

  public Task<DocumentEntry> WriteDocumentAsync(string _namespace, string _key, JToken _data, CancellationToken _ct)
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public Task<DocumentEntry> WriteDocumentAsync(string _namespace, int _key, JToken _data, CancellationToken _ct)
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, string _key, T _data, CancellationToken _ct) where T : notnull
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, int _key, T _data, CancellationToken _ct) where T : notnull
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public Task<DocumentEntry> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct) where T : notnull
    => p_documentStorage.WriteSimpleDocumentAsync(_entryId, _data, _ct);

  public Task<DocumentEntry> WriteSimpleDocumentAsync<T>(int _entryId, T _data, CancellationToken _ct) where T : notnull
    => p_documentStorage.WriteSimpleDocumentAsync(_entryId, _data, _ct);

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  public Task<int> Count(string? _namespace, CancellationToken _ct) => p_documentStorage.Count(_namespace, _ct);

  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  public Task<int> CountSimpleDocuments<T>(CancellationToken _ct) => p_documentStorage.CountSimpleDocuments<T>(_ct);

}
