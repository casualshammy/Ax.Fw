using System;
using System.Net;
using System.Text;

namespace Ax.Fw.Extensions;

public static class WebClientExtensions
{
  /// <summary>
  ///     Forces WebClient to use basic http auth without getting http-401-error
  /// </summary>
  /// <param name="_webClient"></param>
  /// <param name="_username"></param>
  /// <param name="_password"></param>
  public static void ForceBasicAuth(this WebClient _webClient, string _username, string _password)
  {
    string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(_username + ":" + _password));
    _webClient.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
  }
}
