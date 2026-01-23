using Microsoft.AspNetCore.Http;

namespace Ax.Fw.Web.Interfaces;

public interface IRequestToolkit
{
  IResult BadRequest(string _message);
  IResult Conflict(string? _details = null);
  IResult Content(string _obj, string _contentType);
  IResult Forbidden(string _message);
  IResult InternalServerError(string _message);
  IResult Json<T>(T _data);
  IResult NotFound(string? _message = null);
  IResult Ok();
}
