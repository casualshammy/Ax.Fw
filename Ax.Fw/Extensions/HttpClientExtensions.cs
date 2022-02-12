using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions
{
    public static class HttpClientExtensions
    {

        public static async Task<Stream> DownloadWithResume(this HttpClient _client,
           HttpRequestMessage _req,
           TimeSpan _delayBetweenAttempts,
           int _attemptsNum,
           double _minimumConnectionSpeedBytesPerSecond,
           CancellationToken _token)
        {
            return await _client.DownloadWithResume(_req, _ => _delayBetweenAttempts, _attemptsNum, _minimumConnectionSpeedBytesPerSecond, _token);
        }

        public static async Task<Stream> DownloadWithResume(this HttpClient _client,
           HttpRequestMessage _req,
           Func<int, TimeSpan> _delayForAttempt,
           int _attemptsNum,
           double _minimumConnectionSpeedBytesPerSecond,
           CancellationToken _token)
        {
            return (await _client.DownloadWithResumeEx(_req, _delayForAttempt, _attemptsNum, _minimumConnectionSpeedBytesPerSecond, _token)).Stream;
        }

        public static async Task<StreamWithHeaders> DownloadWithResumeEx(this HttpClient _client,
           HttpRequestMessage _req,
           TimeSpan _delayBetweenAttempts,
           int _attemptsNum,
           double _minimumConnectionSpeedBytesPerSecond,
           CancellationToken _token)
        {
            return await _client.DownloadWithResumeEx(_req, _ => _delayBetweenAttempts, _attemptsNum, _minimumConnectionSpeedBytesPerSecond, _token);
        }

        public static async Task<StreamWithHeaders> DownloadWithResumeEx(this HttpClient _client,
           HttpRequestMessage _req,
           Func<int, TimeSpan> _delayForAttempt,
           int _attemptsNum,
           double _minimumConnectionSpeedBytesPerSecond,
           CancellationToken _token)
        {
            using var res = await _client.SendAsync(_req, HttpCompletionOption.ResponseHeadersRead, _token);
            res.EnsureSuccessStatusCode();
            if (!SupportsResume(res))
                return new StreamWithHeaders(await res.Content.ReadAsStreamAsync(), res);
            else
                return new StreamWithHeaders(
                   new ResumingAsyncStream(_req, _client, _delayForAttempt, _attemptsNum, _minimumConnectionSpeedBytesPerSecond),
                   res);
        }

        private static bool SupportsResume(HttpResponseMessage _res)
        {
            if (!_res.Headers.Any(_x => _x.Key.Equals("accept-ranges", StringComparison.OrdinalIgnoreCase)))
                return false;

            var acceptRanges = _res.Headers.FirstOrDefault(_x => _x.Key.Equals("accept-ranges", StringComparison.OrdinalIgnoreCase)).Value.FirstOrDefault();
            if (acceptRanges == default || !acceptRanges.Equals("bytes", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!_res.Content.Headers.Any(_x => _x.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }

    }
}
