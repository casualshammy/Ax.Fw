using Ax.Fw.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Ax.Fw;

public static class Utilities
{
  class CustomComparer<T>(Comparison<T?> _comparison) : Comparer<T>
  {
    private readonly Comparison<T?> p_comparison = _comparison;

    public override int Compare(T? _x, T? _y) => p_comparison(_x, _y);
  }

  class CustomEqualityComparer<T>(Func<T?, T?, bool> _comparison) : IEqualityComparer<T>
  {
    private readonly Func<T?, T?, bool> p_comparison = _comparison;

    public bool Equals(T? _x, T? _y) => p_comparison(_x, _y);

    public int GetHashCode([DisallowNull] T _obj) => _obj.GetHashCode();
  }

  public static string GetRandomString(int _size, bool _onlyLetters)
  {
    var builder = new StringBuilder(_size);
    var chars = _onlyLetters ? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" : "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    for (int i = 0; i < _size; i++)
    {
      var c = chars[Random.Shared.Next(0, chars.Length)];
      builder.Append(c);
    }
    return builder.ToString();
  }

  public static async Task<bool> IsInternetAvailable()
  {
    try
    {
      using var ping = new Ping();
      var pingReply = await ping.SendPingAsync("8.8.8.8", 2000);
      return pingReply != null && (pingReply.Status == IPStatus.Success);
    }
    catch
    {
      return false;
    }
  }

  public static string WordWrap(string _text, int _chunkSize)
  {
    var words = _text.Split(' ').ToList();
    var result = new StringBuilder();
    while (words.Count != 0)
    {
      var sb = new StringBuilder();
      while (words.Count != 0 && sb.Length + 1 + words[0].Length <= _chunkSize)
      {
        sb.Append(" " + words[0]);
        words.RemoveAt(0);
      }
      result.Append(sb.ToString() + "\r\n");
    }
    return result.ToString().TrimEnd('\n').TrimEnd('\r');
  }

  public static string SecureString(string _input)
  {
    var indexesToHide = new int[_input.Length / 2];
    for (int i = 0; i < indexesToHide.Length; i++)
    {
      var newValue = Random.Shared.Next(0, _input.Length);
      while (indexesToHide.Contains(newValue))
        newValue = Random.Shared.Next(0, _input.Length);
      indexesToHide[i] = newValue;
    }
    var builder = new StringBuilder(_input.Length);
    var counter = 0;
    foreach (char c in _input)
    {
      builder.Append(indexesToHide.Contains(counter) ? '*' : c);
      counter++;
    }
    return builder.ToString();
  }

  public static T? GetAttribute<T>(Type _type) where T : Attribute
  {
    var attr = Attribute.GetCustomAttribute(_type, typeof(T)) as T;
    return attr;
  }

  public static IComparer<T> CreateComparer<T>(Comparison<T?> _comparison) => new CustomComparer<T>(_comparison);

  public static IEqualityComparer<T> CreateEqualityComparer<T>(Func<T?, T?, bool> _func) => new CustomEqualityComparer<T>(_func);

}

public static class UtilitiesIO
{
  public static bool IsExecutableAvailable(string _executableNameWithoutExtension, [NotNullWhen(true)] out string? _executablePath)
  {
    _executablePath = null;

    var currentPathUnix = Path.Combine(Directory.GetCurrentDirectory(), _executableNameWithoutExtension);
    if (File.Exists(currentPathUnix))
    {
      _executablePath = currentPathUnix;
      return true;
    }

    var currentPathWindows = Path.Combine(Directory.GetCurrentDirectory(), $"{_executableNameWithoutExtension}.exe");
    if (File.Exists(currentPathWindows))
    {
      _executablePath = currentPathWindows;
      return true;
    }

    var pathVar = Environment.GetEnvironmentVariable("PATH");
    if (pathVar.IsNullOrWhiteSpace())
      return false;

    var folders = pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var folder in folders)
    {
      var pathUnix = Path.Combine(folder, _executableNameWithoutExtension);
      if (File.Exists(pathUnix))
      {
        _executablePath = pathUnix;
        return true;
      }

      var pathWindows = Path.Combine(folder, $"{_executableNameWithoutExtension}.exe");
      if (File.Exists(pathWindows))
      {
        _executablePath = pathWindows;
        return true;
      }
    }

    return false;
  }

}

public static class TaskUtils
{
  public static async Task<Tuple<T1, T2>> WhenAll<T1, T2>(
    Task<T1> _task1,
    Task<T2> _task2)
  {
    await Task.WhenAll(_task1, _task2);
    return new Tuple<T1, T2>(_task1.Result, _task2.Result);
  }

  public static async Task<Tuple<T1, T2, T3>> WhenAll<T1, T2, T3>(
    Task<T1> _task1,
    Task<T2> _task2,
    Task<T3> _task3)
  {
    await Task.WhenAll(_task1, _task2, _task3);
    return new Tuple<T1, T2, T3>(_task1.Result, _task2.Result, _task3.Result);
  }

  public static async Task<Tuple<T1, T2, T3, T4>> WhenAll<T1, T2, T3, T4>(
    Task<T1> _task1,
    Task<T2> _task2,
    Task<T3> _task3,
    Task<T4> _task4)
  {
    await Task.WhenAll(_task1, _task2, _task3, _task4);
    return new Tuple<T1, T2, T3, T4>(_task1.Result, _task2.Result, _task3.Result, _task4.Result);
  }

  public static async Task<Tuple<T1, T2, T3, T4, T5>> WhenAll<T1, T2, T3, T4, T5>(
    Task<T1> _task1,
    Task<T2> _task2,
    Task<T3> _task3,
    Task<T4> _task4,
    Task<T5> _task5)
  {
    await Task.WhenAll(_task1, _task2, _task3, _task4, _task5);
    return new Tuple<T1, T2, T3, T4, T5>(_task1.Result, _task2.Result, _task3.Result, _task4.Result, _task5.Result);
  }

  public static async Task<Tuple<T1, T2, T3, T4, T5, T6>> WhenAll<T1, T2, T3, T4, T5, T6>(
    Task<T1> _task1,
    Task<T2> _task2,
    Task<T3> _task3,
    Task<T4> _task4,
    Task<T5> _task5,
    Task<T6> _task6)
  {
    await Task.WhenAll(_task1, _task2, _task3, _task4, _task5, _task6);
    return new Tuple<T1, T2, T3, T4, T5, T6>(_task1.Result, _task2.Result, _task3.Result, _task4.Result, _task5.Result, _task6.Result);
  }

}