using Ax.Fw.Storage.Data;

namespace Ax.Fw.Storage.Interfaces;

public interface IDocumentStorage : IDisposable
{
  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  void CompactDatabase();

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  int Count(string? _namespace, LikeExpr? _keyLikeExpression);

  /// <summary>
  /// Returns number of simple documents in database
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  int CountSimpleDocuments<T>(LikeExpr? _keyLikeExpression);

  void DeleteDocuments(
    string _namespace,
    KeyAlike? _key,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  void DeleteSimpleDocument<T>(KeyAlike _key) where T : notnull;

  /// <summary>
  /// Flushes temporary file (WAL) to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  void Flush(bool _force);

  IEnumerable<DocumentEntry<T>> ListDocuments<T>(
    string _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  /// <summary>
  /// List documents meta info (without data)
  /// </summary>
  /// <param name="_namespaceLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with namespace starting with "tel:123-456-")</param>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IEnumerable<DocumentEntryMeta> ListDocumentsMeta(
    LikeExpr? _namespaceLikeExpression = null,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  IEnumerable<DocumentEntryMeta> ListDocumentsMeta(
    string? _namespace,
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  /// <summary>
  /// List documents
  /// <para>PAY ATTENTION: If type <see cref="T"/> has not <see cref="SimpleDocumentAttribute"/>, namespace is determined by full name of type <see cref="T"/></para>
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  IEnumerable<DocumentEntry<T>> ListSimpleDocuments<T>(
    LikeExpr? _keyLikeExpression = null,
    DateTimeOffset? _from = null,
    DateTimeOffset? _to = null);

  DocumentEntry<T>? ReadDocument<T>(string _namespace, KeyAlike _key);
  DocumentEntry<T>? ReadSimpleDocument<T>(KeyAlike _key) where T : notnull;
  DocumentEntry<T> WriteDocument<T>(string _namespace, KeyAlike _key, T _data) where T : notnull;
  DocumentEntry<T> WriteSimpleDocument<T>(KeyAlike _key, T _data) where T : notnull;

}