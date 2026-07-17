using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Web.Data;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ax.Fw.Web.Middlewares;

/// <summary>
/// ASP.NET Core middleware that implements a fail-to-ban mechanism to protect endpoints from brute-force attacks.
/// </summary>
/// <remarks>
/// This middleware tracks failed requests (based on HTTP status codes) per IP address and temporarily bans
/// IP addresses that exceed a configurable threshold of failed requests within a time window.
/// 
/// <para>
/// The middleware uses a <see cref="FailToBanAttribute"/> applied to endpoints to configure the ban policy:
/// <list type="bullet">
/// <item><description><see cref="FailToBanAttribute.MaxFailedRequests"/> - Maximum failed requests before ban</description></item>
/// <item><description><see cref="FailToBanAttribute.BanTimeSec"/> - Ban duration in seconds</description></item>
/// <item><description><see cref="FailToBanAttribute.BannedHttpCodes"/> - HTTP status codes considered as failed attempts</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// Banned IPs receive HTTP 429 (Too Many Requests) responses. The ban list is cleaned up every minute,
/// automatically unbanning IPs whose ban period has expired.
/// </para>
/// </remarks>
public class FailToBanMiddleware : IMiddleware
{
  readonly record struct BanInfo(
    int FailedRequests,
    bool IsBanned,
    DateTimeOffset BanUntil);

  record FailReport(
    IPAddress IpAddress,
    int MaxFailedRequests,
    TimeSpan BanTime);

  private readonly ILog p_log;
  private readonly Subject<FailReport> p_failReportSubj = new();
  private readonly ConcurrentDictionary<IPAddress, BanInfo> p_banLut = new();

  public FailToBanMiddleware(
    IReadOnlyLifetime _lifetime,
    ILog _log)
  {
    p_log = _log["fail-to-ban"];

    var scheduler = _lifetime.ToDisposeOnEnded(new EventLoopScheduler());

    p_failReportSubj
      .ObserveOn(scheduler)
      .Subscribe(_failReport =>
      {
        var now = DateTimeOffset.UtcNow;

        p_banLut.AddOrUpdate(
          _failReport.IpAddress,
          _ => new BanInfo(1, false, now + _failReport.BanTime),
          (_, _prev) =>
          {
            var failedRequests = _prev.FailedRequests + 1;
            var isBanned = failedRequests >= _failReport.MaxFailedRequests;
            var banUntil = now + _failReport.BanTime;
            if (banUntil < _prev.BanUntil)
              banUntil = _prev.BanUntil;

            if (isBanned)
            {
              if (!_prev.IsBanned)
                p_log.Warn($"IP address '{_failReport.IpAddress}' is banned until {banUntil:u} after {failedRequests} failed requests");
              else
                p_log.Warn($"IP address '{_failReport.IpAddress}' is re-banned until {banUntil:u} after {failedRequests} failed requests");
            }

            return new BanInfo(failedRequests, isBanned, banUntil);
          });
      }, _lifetime);

    Observable
      .Interval(TimeSpan.FromMinutes(1))
      .ObserveOn(scheduler)
      .Subscribe(__ =>
      {
        var now = DateTimeOffset.UtcNow;

        foreach (var (ip, banInfo) in p_banLut)
        {
          if (banInfo.BanUntil < now)
          {
            p_banLut.TryRemove(ip, out _);

            if (banInfo.IsBanned)
              p_log.Info($"IP address '__{ip}__' is **unbanned** after serving its ban time");
            else
              p_log.Info($"IP address '__{ip}__' is **cleared of suspicion**");
          }
        }
      }, _lifetime);
  }

  public async Task InvokeAsync(HttpContext _ctx, RequestDelegate _next)
  {
    var attr = _ctx.GetEndpoint()?.Metadata.GetMetadata<FailToBanAttribute>();
    if (attr == null)
    {
      await _next(_ctx);
      return;
    }

    var remoteIP = _ctx.Connection.RemoteIpAddress;
    if (remoteIP == null)
    {
      p_log.Warn("IP address is unknown");
      await Results.Problem(detail: "IP address is unknown", statusCode: (int)HttpStatusCode.Forbidden).ExecuteAsync(_ctx);
      return;
    }

    if (p_banLut.TryGetValue(remoteIP, out var banInfo) && banInfo.IsBanned)
    {
      await Results.Problem(detail: "Banned", statusCode: (int)HttpStatusCode.TooManyRequests).ExecuteAsync(_ctx);
      return;
    }

    await _next(_ctx);

    if (!attr.BannedHttpCodes.Contains((HttpStatusCode)_ctx.Response.StatusCode))
      return;

    p_failReportSubj.OnNext(new(remoteIP, attr.MaxFailedRequests, TimeSpan.FromSeconds(attr.BanTimeSec)));
  }

}