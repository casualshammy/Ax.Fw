using System;
using System.Net;
using System.Text;

namespace Ax.Fw.Extensions
{
    public static class WebClientExtensions
    {
        /// <summary>
        ///     Forces WebClient to use basic http auth without getting http-401-error
        /// </summary>
        /// <param name="webClient"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void ForceBasicAuth(this WebClient webClient, string username, string password)
        {
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            webClient.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
        }
    }
}
