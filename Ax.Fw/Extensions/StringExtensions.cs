using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
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

  /// <summary>
  /// Converts a string into a hexadecimal color code using its MD5 hash.
  /// </summary>
  /// <param name="_input">The input string to be converted to a hexadecimal color code.</param>
  /// <param name="_withAlpha">Optional parameter to include alpha channel in the color code. Default is false.</param>
  /// <returns>A hexadecimal color code derived from the MD5 hash of the input string.</returns>
  public static string ExpressStringAsHexColor(
    this string _input, 
    bool _withAlpha = false)
  {
    var hash = MD5.HashData(Encoding.UTF8.GetBytes(_input));
    var result = Convert.ToHexString(hash)[..(_withAlpha ? 4 : 3)];
    return result;
  }

  /// <summary>
  /// Converts a string into a Color object using its MD5 hash.
  /// </summary>
  /// <param name="_input">The input string to be converted to a color.</param>
  /// <param name="_withAlpha">Optional parameter to include alpha channel in the color. Default is false.</param>
  /// <returns>A Color object derived from the MD5 hash of the input string.</returns>
  public static Color ExpressStringAsColor(
    this string _input,
    bool _withAlpha = false)
  {
    var hash = MD5.HashData(Encoding.UTF8.GetBytes(_input));
    return _withAlpha
      ? Color.FromArgb(hash[3], hash[0], hash[1], hash[2])
      : Color.FromArgb(hash[0], hash[1], hash[2]);
  }

  /// <summary>
  /// Calculates the brightness of a given background color in hex format.
  /// Values below 128 are considered dim, above 128 - bright
  /// </summary>
  /// <param name="_hexColor">The background color in hex format (e.g., "#A5B6C7").</param>
  /// <returns>Returns the brightness value calculated using the YIQ formula.</returns>
  /// <remarks>
  /// The method calculates the brightness of the background color using the YIQ formula:
  /// Y = 0.299 * R + 0.587 * G + 0.114 * B.
  /// </remarks>
  public static int GetYiqBrightnessFromHexColor(this string _hexColor)
  {
    if (_hexColor.StartsWith('#'))
      _hexColor = _hexColor[1..];

    var r = Convert.ToInt32(_hexColor[..2], 16);
    var g = Convert.ToInt32(_hexColor.Substring(2, 2), 16);
    var b = Convert.ToInt32(_hexColor.Substring(4, 2), 16);

    return GetYiqBrightnessFromColor(Color.FromArgb(r, g, b));
  }

  /// <summary>
  /// Calculates the brightness of a given background color in hex format.
  /// Values below 128 are considered dim, above 128 - bright
  /// </summary>
  /// <param name="_color">The background color.</param>
  /// <returns>Returns the brightness value calculated using the YIQ formula.</returns>
  /// <remarks>
  /// The method calculates the brightness of the background color using the YIQ formula:
  /// Y = 0.299 * R + 0.587 * G + 0.114 * B.
  /// </remarks>
  public static int GetYiqBrightnessFromColor(this Color _color)
  {
    var yiq = ((_color.R * 299) + (_color.G * 587) + (_color.B * 114)) / 1000;
    return yiq;
  }

}
