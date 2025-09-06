using Ax.Fw.Storage.JsonConverters;
using System.Globalization;
using System.Text.Json.Serialization;

[JsonConverter(typeof(KeyAlikeJsonConverter))]
public class KeyAlike
{
  public KeyAlike(string _key)
  {
    Key = _key;
  }

  public string Key { get; }

  public static implicit operator KeyAlike(string _key) => new(_key);
  public static implicit operator KeyAlike(int _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(uint _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(long _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(ulong _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(float _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(double _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(Int128 _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(UInt128 _key) => new(_key.ToString(CultureInfo.InvariantCulture));
  public static implicit operator KeyAlike(Guid _key) => new(_key.ToString());

}