using System.IO;
using System.Threading;

namespace Ax.Fw.Cache.Parts;

internal class FileCacheStoreTask
{
  public FileCacheStoreTask(Stream _data, string _key, CancellationToken _token)
  {
    Data = _data;
    Key = _key;
    Token = _token;
  }

  public Stream Data { get; }
  public string Key { get; }
  public CancellationToken Token { get; }

}
