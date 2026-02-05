using Ax.Fw.Storage.Data;
using System.Diagnostics.CodeAnalysis;

namespace Ax.Fw.Storage.Interfaces;

public interface IBlobStorage : IGenericStorage, IDisposable
{
  void DeleteBlobs(string _namespace, KeyAlike? _key, DateTimeOffset? _from = null, DateTimeOffset? _to = null);
  IEnumerable<BlobEntryMeta> ListBlobsMeta(string? _namespace, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null);
  IEnumerable<BlobEntryMeta> ListBlobsMeta(LikeExpr? _namespaceLikeExpression = null, LikeExpr? _keyLikeExpression = null, DateTimeOffset? _from = null, DateTimeOffset? _to = null);
  bool TryReadBlob(string _namespace, KeyAlike _key, [NotNullWhen(true)] out BlobStream? _outputData, [NotNullWhen(true)] out BlobEntryMeta? _meta);
  bool TryReadBlob(string _namespace, KeyAlike _key, [NotNullWhen(true)] out byte[]? _outputData, [NotNullWhen(true)] out BlobEntryMeta? _meta);
  Task<BlobEntryMeta> WriteBlobAsync(string _namespace, KeyAlike _key, Stream _data, long _size, CancellationToken _ct);
  Task<BlobEntryMeta> WriteBlobAsync(string _namespace, KeyAlike _key, byte[] _data, CancellationToken _ct);
}
