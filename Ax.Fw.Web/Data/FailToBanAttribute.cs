using System.Net;

namespace Ax.Fw.Web.Data;

[AttributeUsage(AttributeTargets.Method)]
public class FailToBanAttribute : Attribute
{
  public FailToBanAttribute(int _maxFailedRequests = 10, params HttpStatusCode[] _bannedHttpCodes)
  {
    MaxFailedRequests = _maxFailedRequests;
    BannedHttpCodes = _bannedHttpCodes.ToHashSet();
  }

  public int MaxFailedRequests { get; }
  public IReadOnlySet<HttpStatusCode> BannedHttpCodes { get; }
}