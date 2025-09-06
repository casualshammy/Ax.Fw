using System;
using System.Collections.Generic;

namespace Ax.Fw.Mathematics;

public class MedianFilter
{
  private readonly int p_windowSize;
  private readonly int p_medianIndex;
  private readonly int p_halfWindowSize;

  public MedianFilter(int _windowSize)
  {
    if (_windowSize <= 0 || _windowSize % 2 == 0)
      throw new ArgumentOutOfRangeException(nameof(_windowSize), $"Window size must be bigger than 0 and odd");

    p_windowSize = _windowSize;
    p_medianIndex = (p_windowSize - 1) / 2;
    p_halfWindowSize = (int)Math.Floor(p_windowSize / 2f);
  }

  public int[] Calculate(int[] _rawValues)
  {
    var length = _rawValues.Length;
    var result = new int[length];

    var list = new List<int>(p_windowSize);
    for (int i = 0; i < length; i++)
    {
      list.Clear();

      for (var part = p_halfWindowSize; part > 0; part--)
        list.Add(i - part < 0 ? _rawValues[0] : _rawValues[i - part]);

      list.Add(_rawValues[i]);

      for (var part = 1; part <= p_halfWindowSize; part++)
        list.Add(i + part > length - 1 ? _rawValues[length - 1] : _rawValues[i + part]);

      list.Sort();
      var value = list[p_medianIndex];
      result[i] = value;
    }

    return result;
  }

}
