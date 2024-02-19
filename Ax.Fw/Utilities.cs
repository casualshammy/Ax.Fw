using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

  public static IEnumerable<Type> GetTypesWithAttr<T>(bool _inherit) where T : Attribute
  {
    return AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(_x => _x.GetTypes())
        .Where(_x => _x.IsDefined(typeof(T), _inherit));
  }

  public static T? GetAttribute<T>(Type _type) where T : Attribute
  {
    var attr = Attribute.GetCustomAttribute(_type, typeof(T)) as T;
    return attr;
  }

  public static IEnumerable<Type> GetTypesOf<T>()
  {
    return AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(_x => _x.GetTypes())
        .Where(_x => typeof(T).IsAssignableFrom(_x));
  }

  public static IComparer<T> CreateComparer<T>(Comparison<T?> _comparison) => new CustomComparer<T>(_comparison);

  public static IEqualityComparer<T> CreateEqualityComparer<T>(Func<T?, T?, bool> _func) => new CustomEqualityComparer<T>(_func);

}
