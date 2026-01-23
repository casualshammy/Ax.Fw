using Ax.Fw.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json.Serialization;

namespace Ax.Fw.Web.Modules.RequestToolkit;

internal class RequestToolkitImpl : IRequestToolkit
{
  private readonly JsonSerializerContext p_jsonCtx;

  public RequestToolkitImpl(
    JsonSerializerContext _jsonCtx)
  {
    p_jsonCtx = _jsonCtx;
  }

  public IResult Ok() => Results.Ok();
  public IResult Json<T>(T _data) => Results.Json(_data, p_jsonCtx);
  public IResult Content(string _obj, string _contentType) => Results.Content(_obj, _contentType);
  public IResult InternalServerError(string _message) => Results.Problem(_message, statusCode: (int)HttpStatusCode.InternalServerError);
  public IResult BadRequest(string _message) => Results.Problem(_message, statusCode: (int)HttpStatusCode.BadRequest);
  public IResult Forbidden(string _message) => Results.Problem(_message, statusCode: (int)HttpStatusCode.Forbidden);
  public IResult NotFound(string? _message = null) => Results.Problem(_message, statusCode: (int)HttpStatusCode.NotFound);
  public IResult Conflict(string? _details = null) => Results.Problem(_details, statusCode: (int)HttpStatusCode.Conflict);

}
