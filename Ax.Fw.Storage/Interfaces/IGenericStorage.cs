using Ax.Fw.Storage.Data;

namespace Ax.Fw.Storage.Interfaces;

public interface IGenericStorage
{
  /// <summary>
  /// Gets the full path of the storage file.
  /// </summary>
  string FilePath { get; }

  /// <summary>
  /// Rebuilds the database file, repacking it into a minimal amount of disk space
  /// </summary>
  void CompactDatabase();

  /// <summary>
  /// Returns number of documents in database
  /// </summary>
  /// <param name="_keyLikeExpression">SQL 'LIKE' expression (ex: "tel:123-456-%" will return all docs with key starting with "tel:123-456-")</param>
  long Count(string? _namespace, LikeExpr? _keyLikeExpression);

  /// <summary>
  /// Flushes temporary file (WAL) to main database file
  /// </summary>
  /// <param name="_force">If true, forcefully performs full flush and then truncates temporary file to zero bytes</param>
  /// <returns></returns>
  void Flush(bool _force);
}
