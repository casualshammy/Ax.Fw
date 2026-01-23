using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Web.Data;
using Ax.Fw.Web.Interfaces;
using Ax.Fw.Web.Middlewares;
using Ax.Fw.Web.Modules.RequestId;
using Ax.Fw.Web.Modules.RequestToolkit;
using Ax.Fw.Web.Modules.ScopedLog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Ax.Fw.Web.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddCustomRequestId(this IServiceCollection _services)
  {
    _services.AddScoped<IRequestId>(_sp =>
    {
      var guid = Guid.NewGuid();
      return new RequestIdImpl(guid);
    });

    return _services;
  }

  public static IServiceCollection AddCustomRequestLog(this IServiceCollection _services)
  {
    _services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

    _services.AddScoped<IScopedLog>(_sp =>
    {
      var log = _sp.GetRequiredService<ILog>();
      var reqId = _sp.GetRequiredService<IRequestId>();
      var httpCtx = _sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
      var ctrlInfo = httpCtx?.GetEndpoint()?.Metadata.GetMetadata<RestControllerInfo>();

      return ctrlInfo != null
        ? new ScopedLogImpl(log[ctrlInfo.Type][reqId.Id.ToString()[..8]])
        : new ScopedLogImpl(log["unknown-ctrl"][reqId.Id.ToString()[..8]]);
    });

    return _services;
  }

  public static IServiceCollection AddRequestToolkit(
    this IServiceCollection _services,
    JsonSerializerContext _jsonCtx)
  {
    _services.AddSingleton<IRequestToolkit>(_sp =>
    {
      return new RequestToolkitImpl(_jsonCtx);
    });

    return _services;
  }

  public static IServiceCollection AddCorsMiddleware(
    this IServiceCollection _services,
    IReadOnlySet<string> _allowedOrigins,
    IReadOnlySet<string> _allowedMethods,
    IReadOnlySet<string> _allowedHeaders,
    bool _allowedCredentials)
  {
    _services.AddSingleton(_sp =>
    {
      var mw = new CorsMiddleware(_allowedOrigins, _allowedMethods, _allowedHeaders, _allowedCredentials);

      return mw;
    });

    return _services;
  }

  public static IServiceCollection AddCustomProblemDetails(this IServiceCollection _services)
  {
    _services.AddProblemDetails(_opt =>
    {
      _opt.CustomizeProblemDetails = HandleProblemDetails;
    });

    return _services;
  }

  private static void HandleProblemDetails(ProblemDetailsContext _problemCtx)
  {
    var httpCtx = _problemCtx.HttpContext;
    var reqCtx = httpCtx.RequestServices.GetRequiredService<IRequestId>();

    httpCtx.Features.Set(new ProblemFeature(_problemCtx.ProblemDetails.Detail ?? string.Empty));

    _problemCtx.ProblemDetails.Instance = $"{httpCtx.Request.Method} {httpCtx.Request.Path}";
    _problemCtx.ProblemDetails.Extensions.TryAdd("requestId", reqCtx.Id.ToString());
  }

}
