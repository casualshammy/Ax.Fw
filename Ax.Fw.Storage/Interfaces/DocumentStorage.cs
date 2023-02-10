using Ax.Fw.Storage.Data;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.Interfaces;

public abstract class DocumentStorage : DisposableStack
{
  public abstract Task CompactDatabase(CancellationToken _ct);
  public abstract Task<int> Count(string? _namespace, CancellationToken _ct);
  public abstract Task<int> CountSimpleDocuments<T>(CancellationToken _ct);
  public abstract Task DeleteDocumentsAsync(string _namespace, string? _key, DateTimeOffset? _from, DateTimeOffset? _to, CancellationToken _ct);
  public abstract Task DeleteSimpleDocumentAsync<T>(string _entryId, CancellationToken _ct) where T : notnull;
  public abstract Task DeleteSimpleDocumentAsync<T>(int _entryId, CancellationToken _ct) where T : notnull;
#pragma warning disable CS8424 // The EnumeratorCancellationAttribute will have no effect. The attribute is only effective on a parameter of type CancellationToken in an async-iterator method returning IAsyncEnumerable
  public abstract IAsyncEnumerable<DocumentEntry> ListDocumentsAsync(string _namespace, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct);
  public abstract IAsyncEnumerable<DocumentEntryMeta> ListDocumentsMetaAsync(string? _namespace, DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct);
  public abstract IAsyncEnumerable<DocumentTypedEntry<T>> ListSimpleDocumentsAsync<T>(DateTimeOffset? _from, DateTimeOffset? _to, [EnumeratorCancellation] CancellationToken _ct);
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