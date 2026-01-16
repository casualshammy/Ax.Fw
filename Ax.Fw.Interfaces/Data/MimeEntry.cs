using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ax.Fw.SharedTypes.Data;

public sealed record MimeEntry
{
  private static readonly ConcurrentDictionary<string, MimeEntry> p_entryPerMime = [];
  private static readonly ConcurrentDictionary<string, MimeEntry> p_entryPerExt = [];

  public MimeEntry(string[] _extensions, string _mime)
  {
    Extensions = _extensions;
    Mime = _mime;

    p_entryPerMime.AddOrUpdate(_mime, this, (_ ,_) => this);

    foreach (var ext in _extensions)
      p_entryPerExt.AddOrUpdate(ext, this, (_, _) => this);
  }

  public string[] Extensions { get; init; }
  public string Mime { get; init; }

  public static IReadOnlyDictionary<string, MimeEntry> ByMime => p_entryPerMime;
  public static IReadOnlyDictionary<string, MimeEntry> ByExtension => p_entryPerExt;

}
