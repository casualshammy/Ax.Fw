namespace Ax.Fw.Extensions;

public static class NumberExtensions
{
  public static int LimitBetween(this int _value, int _minValue, int _maxValue)
  {
    if (_value > _maxValue)
      return _maxValue;
    if (_value < _minValue)
      return _minValue;

    return _value;
  }


  public static string ToHumanBytes(this long _value, int _fractions = 0)
  {
    if (_value > 1024 * 1024 * 1024)
      return (_value / 1024f / 1024f / 1024f).ToString($"F{_fractions}") + " GB";
    if (_value > 1024 * 1024)
      return (_value / 1024f / 1024f).ToString($"F{_fractions}") + " MB";
    if (_value > 1024)
      return (_value / 1024f).ToString($"F{_fractions}") + " KB";

    return $"{_value} B";
  }

}