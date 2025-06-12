using System;

namespace Ax.Fw.Cache.Parts;

internal class SyncCacheEntry<TValue>
{
  public SyncCacheEntry(DateTimeOffset _validUntil, TValue _data)
  {
    ValidUntil = _validUntil;
    Data = _data;
  }

  public DateTimeOffset ValidUntil { get; }
  public TValue Data { get; }
}
