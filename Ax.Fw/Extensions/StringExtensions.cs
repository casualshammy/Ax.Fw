using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Ax.Fw.Extensions;

public static class StringExtensions
{
  public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? _string) => string.IsNullOrWhiteSpace(_string);
  public static bool IsNullOrEmpty([NotNullWhen(false)] this string? _string) => string.IsNullOrEmpty(_string);

  public static bool TryParseByteValue([NotNullWhen(true)] this string? _string, out long _value)
  {
    _value = 0;

    if (_string == null || _string.Length == 0)
      return false;

    if (long.TryParse(_string, out var longValue))
    {
      _value = longValue;
      return true;
    }

    var number = _string[..^1];
    if (!long.TryParse(number, out longValue))
      return false;

    var lastChar = _string[^1..].ToLowerInvariant();
    if (lastChar == "k")
    {
      _value = longValue * 1024;
      return true;
    }
    if (lastChar == "m")
    {
      _value = longValue * 1024 * 1024;
      return true;
    }
    if (lastChar == "g")
    {
      _value = longValue * 1024 * 1024 * 1024;
      return true;
    }

    return false;
  }

  public static string ToSafeFilePath(this string _str)
  {
    var sb = new StringBuilder(_str);
    foreach (var c in Path.GetInvalidFileNameChars())
      sb.Replace(c, '-');

    return sb.ToString();
  }

  public static string Truncate(this string _str, int _maxLength)
  {
    if (_str.Length <= _maxLength)
      return _str;

    return _str[.._maxLength];
  }

}
