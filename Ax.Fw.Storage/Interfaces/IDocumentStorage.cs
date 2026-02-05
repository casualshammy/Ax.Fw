using Ax.Fw.Storage.Data;

namespace Ax.Fw.Storage.Interfaces;

public interface IDocumentStorage : IGenericStorage, IDisposable
{
  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  long CountSimpleDocuments<T>(LikeExpr? _keyLikeExpression);

  void DeleteDocuments(
    string _namespace,
    KeyAlike? _key,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  void DeleteSimpleDocument<T>(KeyAlike _key) where T : notnull;

  IEnumerable<BlobEntry<T>> ListDocuments<T>(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  /// <param name="_namespaceLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with namespace starting with "tel:123-456-")</param>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IEnumerable<BlobEntryMeta> ListDocumentsMeta(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  IEnumerable<BlobEntryMeta> ListDocumentsMeta(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IEnumerable<BlobEntry<T>> ListSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  BlobEntry<T>? ReadDocument<T>(string _namespace, KeyAlike _key);
  BlobEntry<T>? ReadSimpleDocument<T>(KeyAlike _key) where T : notnull;
  BlobEntry<T> WriteDocument<T>(string _namespace, KeyAlike _key, T _data) where T : notnull;
  BlobEntry<T> WriteSimpleDocument<T>(KeyAlike _key, T _data) where T : notnull;

}