using Ax.Fw.Extensions;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Interfaces;
using Newtonsoft.Json.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.StorageTypes;

public class DocumentStorageWithRetentionRules : DocumentStorage
{
  private readonly DocumentStorage p_documentStorage;

  internal DocumentStorageWithRetentionRules(
    DocumentStorage _documentStorage,
    TimeSpan? _documentMaxAgeFromCreation = null,
    TimeSpan? _documentMaxAgeFromLastChange = null,
    TimeSpan? _scanInterval = null,
    Action<DocumentEntryMeta>? _onDocDeleteCallback = null)
  {
    p_documentStorage = ToDispose(_documentStorage);

    ToDispose(Pool<EventLoopScheduler>.Get(out var scheduler));

    var subscription = Observable
      .Interval(_scanInterval ?? TimeSpan.FromMinutes(10), scheduler)
      .StartWithDefault()
      .SelectAsync(async (_, _ct) =>
      {
        var now = DateTimeOffset.UtcNow;
        var docsToDelete = new HashSet<DocumentEntryMeta>();

        await foreach (var doc in _documentStorage.ListDocumentsMetaAsync(null, null, null, _ct).ConfigureAwait(false))
        {
          var docAge = now - doc.Created;
          var docLastModifiedAge = now - doc.LastModified;
          if (_documentMaxAgeFromCreation != null && docAge > _documentMaxAgeFromCreation)
            docsToDelete.Add(doc);
          else if (_documentMaxAgeFromLastChange != null && docLastModifiedAge > _documentMaxAgeFromLastChange)
            docsToDelete.Add(doc);
        }

        foreach (var doc in docsToDelete)
        {
          await _documentStorage.DeleteDocumentsAsync(doc.Namespace, doc.Key, null, null, _ct);
          _onDocDeleteCallback?.Invoke(doc);
        }
      }, scheduler)
      .Subscribe();

    ToDispose(subscription);
  }

  public override Task CompactDatabase(CancellationToken _ct) => p_documentStorage.CompactDatabase(_ct);

  public override Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from, DateTimeOffset? _to, CancellationToken _ct)
    => p_documentStorage.DeleteDocumentsAsync(_namespace, _key, _from, _to, _ct);

  public override Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct)
    => p_documentStorage.DeleteSimpleDocumentAsync<T>(_entryId, _ct);

  public override Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct)
    => p_documentStorage.DeleteSimpleDocumentAsync<T>(_entryId, _ct);

#pragma warning disable CS8424
  public override IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(string _namespace, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct)
    => p_documentStorage.ListDocumentsAsync(_namespace, _from, _to, _ct);

  public override IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(string? _namespace, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct)
    => p_documentStorage.ListDocumentsMetaAsync(_namespace, _from, _to, _ct);

  public override IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct)
    => p_documentStorage.ListSimpleDocumentsAsync<T>(_from, _to, _ct);
#pragma warning restore CS8424

  public override Task<DocumentEntry?> ReadDocumentAsync(string _namespace, string _key, CancellationToken _ct)
    => p_documentStorage.ReadDocumentAsync(_namespace, _key, _ct);

  public override Task<DocumentEntry?> ReadDocumentAsync(string _namespace, int _key, CancellationToken _ct)
    => p_documentStorage.ReadDocumentAsync(_namespace, _key, _ct);

  public override Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct)
    => p_documentStorage.ReadSimpleDocumentAsync<T>(_entryId, _ct);

  public override Task<DocumentTypedEntry<T>?> ReadSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct)
    => p_documentStorage.ReadSimpleDocumentAsync<T>(_entryId, _ct);

  public override Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, string _key, CancellationToken _ct)
    => p_documentStorage.ReadTypedDocumentAsync<T>(_namespace, _key, _ct);

  public override Task<DocumentTypedEntry<T>?> ReadTypedDocumentAsync<T>(string _namespace, int _key, CancellationToken _ct)
    => p_documentStorage.ReadTypedDocumentAsync<T>(_namespace, _key, _ct);

  public override Task<DocumentEntry> WriteDocumentAsync(string _namespace, string _key, JToken _data, CancellationToken _ct)
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public override Task<DocumentEntry> WriteDocumentAsync(string _namespace, int _key, JToken _data, CancellationToken _ct)
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public override Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, string _key, T _data, CancellationToken _ct)
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public override Task<DocumentEntry> WriteDocumentAsync<T>(string _namespace, int _key, T _data, CancellationToken _ct)
    => p_documentStorage.WriteDocumentAsync(_namespace, _key, _data, _ct);

  public override Task<DocumentEntry> WriteSimpleDocumentAsync<T>(string _entryId, T _data, CancellationToken _ct)
    => p_documentStorage.WriteSimpleDocumentAsync(_entryId, _data, _ct);

  public override Task<DocumentEntry> WriteSimpleDocumentAsync<T>(int _entryId, T _data, CancellationToken _ct)
    => p_documentStorage.WriteSimpleDocumentAsync(_entryId, _data, _ct);

  public override Task<int> Count(string? _namespace, CancellationToken _ct) => p_documentStorage.Count(_namespace, _ct);

  public override Task<int> CountSimpleDocuments<T>(CancellationToken _ct) => p_documentStorage.CountSimpleDocuments<T>(_ct);

}
