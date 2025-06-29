﻿using System;
using System.Runtime.InteropServices;

namespace Ax.Fw.Extensions;

public static class ArrayExtensions
{
  public static void Deconstruct<T>(
    this T[] _array,
    out T _item0,
    out T _item1)
  {
    if (_array.Length < 2)
      throw new InvalidOperationException($"Array contains insufficient number of elements: {_array.Length}");

    _item0 = _array[0];
    _item1 = _array[1];
  }

  public static void Deconstruct<T>(
    this T[] _array,
    out T _item0,
    out T _item1,
    out T _item2)
  {
    if (_array.Length < 3)
      throw new InvalidOperationException($"Array contains insufficient number of elements: {_array.Length}");

    _item0 = _array[0];
    _item1 = _array[1];
    _item2 = _array[2];
  }

  public static void Deconstruct<T>(
    this T[] _array,
    out T _item0,
    out T _item1,
    out T _item2,
    out T _item3)
  {
    if (_array.Length < 4)
      throw new InvalidOperationException($"Array contains insufficient number of elements: {_array.Length}");

    _item0 = _array[0];
    _item1 = _array[1];
    _item2 = _array[2];
    _item3 = _array[3];
  }

  public static void Deconstruct<T>(
    this T[] _array,
    out T _item0,
    out T _item1,
    out T _item2,
    out T _item3,
    out T _item4)
  {
    if (_array.Length < 5)
      throw new InvalidOperationException($"Array contains insufficient number of elements: {_array.Length}");

    _item0 = _array[0];
    _item1 = _array[1];
    _item2 = _array[2];
    _item3 = _array[3];
    _item4 = _array[4];
  }

}
