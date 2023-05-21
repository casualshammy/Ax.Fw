using System.Diagnostics.CodeAnalysis;

namespace Ax.Fw.Extensions;

public static class StringExtensions
{
  public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? _string) => string.IsNullOrWhiteSpace(_string);
  public static bool IsNullOrEmpty([NotNullWhen(false)] this string? _string) => string.IsNullOrEmpty(_string);
}
