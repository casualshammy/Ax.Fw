using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Interfaces;
using Newtonsoft.Json.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.StorageTypes;

public class DocumentStorageWithRetentionRules : IDocumentStorage
{
  private readonly IDocumentStorage p_documentStorage;

  internal DocumentStorageWithRetentionRules(
    IDocumentStorage _documentStorage,
    IReadOnlyLifetime _lifetime,
    TimeSpan? _documentMaxAgeFromCreation = null,
    TimeSpan? _documentMaxAgeFromLastChange = null,
    TimeSpan? _scanInterval = null)
  {
    p_documentStorage = _documentStorage;

    _lifetime.DisposeOnCompleted(Pool<EventLoopScheduler>.Get(out var scheduler));

    Observable
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
          await _documentStorage.DeleteDocumentsAsync(doc.Namespace, doc.Key, null, null, _ct);
      }, scheduler)
      .Subscribe(_lifetime);
  }

  public Task CompactDatabase(CancellationToken _ct) => p_documentStorage.CompactDatabase(_ct);

  public Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from, DateTimeOffset? _to, CancellationToken _ct)
    => p_documentStorage.DeleteDocumentsAsync(_namespace, _key, _from, _to, _ct);

  public Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull
    => p_documentStorage.DeleteSimpleDocumentAsync<T>(_entryId, _ct);

  public Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull
    => p_documentStorage.DeleteSimpleDocumentAsync<T>(_entryId, _ct);

#pragma warning disable CS8424
  public IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(string _namespace, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct)
    => p_documentStorage.ListDocumentsAsync(_namespace, _from, _to, _ct);

  public IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(string? _namespace, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct)
    => p_documentStorage.ListDocumentsMetaAsync(_namespace, _from, _to, _ct);

  public IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct)
    => p_documentStorage.ListSimpleDocumentsAsync<T>(_from, _to, _ct);
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

}
