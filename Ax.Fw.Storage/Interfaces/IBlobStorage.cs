using Ax.Fw.Storage.Data;

namespace Ax.Fw.Storage.Interfaces;

public interface IBlobStorage : IDisposable
{
  void CompactDatabase();
  int Count(string? _namespace, LikeExpr? _keyLikeExpression);
  void DeleteDocuments(string _namespace, KeyAlike? _key, DateTimeOffset? _from = null, DateTimeOffset? _to = null);
  void Flush(bool _force);
  IEnumerable<DocumentEntryMeta> ListDocumentsMeta(string? _namespace, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null);
  IEnumerable<DocumentEntryMeta> ListDocumentsMeta(LikeExpr? _namespaceLikeExpression = null, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null);
  Task<DocumentEntryMeta?> ReadDocumentAsync(string _namespace, KeyAlike _key, Stream _outputStream, CancellationToken _ct);
  Task<DocumentEntryMeta> WriteDocumentAsync(string _namespace, KeyAlike _key, Stream _data, CancellationToken _ct);
  Task<DocumentEntryMeta> WriteDocumentAsync(string _namespace, KeyAlike _key, byte[] _data, CancellationToken _ct);
}
