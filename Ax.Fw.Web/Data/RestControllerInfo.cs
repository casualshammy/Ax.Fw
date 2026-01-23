namespace Ax.Fw.Web.Data;

public sealed class RestControllerInfo
{
  public RestControllerInfo(
    string _type,
    string _tag)
  {
    Type = _type;
    Tag = _tag;
  }

  public string Type { get; init; }
  public string Tag { get; init; }
}
