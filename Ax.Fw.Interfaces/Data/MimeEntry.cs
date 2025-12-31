using System.Collections.Generic;

namespace Ax.Fw.SharedTypes.Data;

public sealed record MimeEntry
{
  private static readonly HashSet<MimeEntry> p_allEntries = [];

  public MimeEntry(string[] _extensions, string _mime)
  {
    Extensions = _extensions;
    Mime = _mime;

    p_allEntries.Add(this);
  }

  public string[] Extensions { get; init; }
  public string Mime { get; init; }

  public static IReadOnlySet<MimeEntry> AllEntries { get; } = p_allEntries;
}
