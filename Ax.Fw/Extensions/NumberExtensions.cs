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
}