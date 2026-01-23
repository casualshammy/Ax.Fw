using System.Net;

namespace Ax.Fw.Web.Data;

[AttributeUsage(AttributeTargets.Method)]
public class FailToBanAttribute : Attribute
{
  public FailToBanAttribute(
    int _maxFailedRequests = 10,
    int _banTimeSec = 10 * 60,
    params HttpStatusCode[] _bannedHttpCodes)
  {
    if (_maxFailedRequests < 2)
      throw new ArgumentOutOfRangeException(nameof(_maxFailedRequests), "Must be at least 2");
    if (_banTimeSec < 1)
      throw new ArgumentOutOfRangeException(nameof(_banTimeSec), "Must be at least 1 sec");

    MaxFailedRequests = _maxFailedRequests;
    BanTimeSec = _banTimeSec;
    BannedHttpCodes = _bannedHttpCodes.ToHashSet();
  }

  public int MaxFailedRequests { get; }
  public int BanTimeSec { get; }
  public IReadOnlySet<HttpStatusCode> BannedHttpCodes { get; }
}