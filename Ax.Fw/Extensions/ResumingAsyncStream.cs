using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions;

public class ResumingAsyncStream : Stream
{
  private const int TOO_SLOW_TOLERANCE = 3;
  private readonly long p_length;
  private readonly HttpClient p_client;
  private readonly Func<int, TimeSpan> p_delayFunc;
  private readonly int p_numberOfRetries;
  private readonly double p_minSpeedBytesSec;
  private readonly HttpMethod p_httpMethod;
  private readonly Uri p_uri;

  private int p_attempt = 0;
  private int p_tooSlowCount = 0;
  private long p_position = 0;
  private Stream? p_networkStream = null;

  public ResumingAsyncStream(HttpRequestMessage _req,
     HttpClient _client,
     Func<int, TimeSpan> _delayForAttempt,
     int _numberOfRetries,
     double _minimumConnectionSpeedBytesPerSecond)
  {
    var req = new HttpRequestMessage(_req.Method, _req.RequestUri);
    if (req.RequestUri == null)
      throw new ArgumentNullException(nameof(_req.RequestUri));

    using var res = _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).Result;
    var contentLengthHeader = res.Content.Headers.FirstOrDefault(_x => _x.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)).Value.FirstOrDefault();
    if (!long.TryParse(contentLengthHeader, out p_length) || p_length < 0)
      throw new InvalidDataException($"Content-Length is invalid or zero! Header value: ({contentLengthHeader})");

    p_client = _client;
    p_delayFunc = _delayForAttempt;
    p_numberOfRetries = _numberOfRetries;
    p_minSpeedBytesSec = _minimumConnectionSpeedBytesPerSecond;
    p_httpMethod = req.Method;
    p_uri = req.RequestUri;
  }

  public override bool CanRead => true;

  public override bool CanSeek => false;

  public override bool CanWrite => false;

  public override long Length => p_length;

  public override long Position { get => p_position; set => throw new NotImplementedException(); }

  public override void Flush() { }

  public override int Read(byte[] buffer, int offset, int count)
  {
    return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
  }

  public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken _token)
  {
    if (p_attempt >= p_numberOfRetries)
      throw new InvalidOperationException($"Can't download file! Total read bytes: {p_position}; content length: {p_length})");
    if (p_tooSlowCount >= TOO_SLOW_TOLERANCE)
    {
      p_networkStream?.Dispose();
      p_networkStream = null;
      throw new InvalidOperationException($"Too slow connection!");
    }

    if (p_position >= p_length)
      return 0;

    while (p_attempt < p_numberOfRetries && p_position < p_length && !_token.IsCancellationRequested)
    {
      HttpRequestMessage? req = null;
      HttpResponseMessage? res = null;

      try
      {
        if (p_networkStream == null || !p_networkStream.CanRead)
        {
          req = new HttpRequestMessage(p_httpMethod, p_uri);
          req.Headers.Add("range", $"bytes={p_position}-");

          res = await p_client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, _token);
          if (res.StatusCode != System.Net.HttpStatusCode.PartialContent)
            throw new InvalidOperationException(
               $"Server supposed to support partial download returned code {res.StatusCode} (must be {System.Net.HttpStatusCode.PartialContent})!");

          p_networkStream = await res.Content.ReadAsStreamAsync();
        }

        var sw = Stopwatch.StartNew();
        int bytesRead = await p_networkStream.ReadAsync(buffer, offset, count, _token);
        p_position += bytesRead;
        var currentSpeed = bytesRead / sw.Elapsed.TotalSeconds;
        if (currentSpeed < p_minSpeedBytesSec)
          p_tooSlowCount++;
        else
          p_tooSlowCount = 0;

        p_attempt = 0;
        if (p_position >= p_length)
        {
          p_networkStream?.Dispose();
          p_networkStream = null;
        }
        return bytesRead;
      }
      catch (OperationCanceledException)
      {
        p_networkStream?.Dispose();
        p_networkStream = null;
        req?.Dispose();
        res?.Dispose();
        throw;
      }
      catch
      {
        p_networkStream?.Dispose();
        p_networkStream = null;
        req?.Dispose();
        res?.Dispose();
        await Task.Delay((int)p_delayFunc(p_attempt++).TotalMilliseconds, _token);
      }
    }
    throw new InvalidOperationException($"Can't download file! Total read bytes: {p_position}; content length: {p_length})");
  }

  public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

  public override void SetLength(long value)
  {
    throw new NotImplementedException();
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    throw new NotImplementedException();
  }

}
