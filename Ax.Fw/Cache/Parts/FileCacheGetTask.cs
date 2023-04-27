using System.Threading;

namespace Ax.Fw.Cache.Parts;

internal class FileCacheGetTask
{
  public FileCacheGetTask(string _key, CancellationToken _token)
  {
    Key = _key;
    Token = _token;
  }

  public string Key { get; }
  public CancellationToken Token { get; }

}
