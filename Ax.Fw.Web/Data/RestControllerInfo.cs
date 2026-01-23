namespace Ax.Fw.Web.Data;

internal sealed class RestControllerInfo
{
  public RestControllerInfo(
    string _type,
    string _swaggerTag)
  {
    Type = _type;
    SwaggerTag = _swaggerTag;
  }

  public string Type { get; init; }
  public string SwaggerTag { get; init; }
}
