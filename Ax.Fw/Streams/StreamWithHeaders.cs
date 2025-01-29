using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Ax.Fw.Streams;

public class StreamWithHeaders
{
  public StreamWithHeaders(Stream _stream, HttpContentHeaders _headers, HttpStatusCode _statusCode, string? _reasonPhrase)
  {
    Stream = _stream;
    Headers = _headers;
    StatusCode = _statusCode;
    ReasonPhrase = _reasonPhrase;
  }

  public StreamWithHeaders(Stream _stream, HttpResponseMessage _httpResponse)
  {
    Stream = _stream;
    Headers = _httpResponse.Content.Headers;
    StatusCode = _httpResponse.StatusCode;
    ReasonPhrase = _httpResponse.ReasonPhrase;
  }

  public Stream Stream { get; }
  public HttpContentHeaders Headers { get; }
  public HttpStatusCode StatusCode { get; }
  public string? ReasonPhrase { get; }
}
