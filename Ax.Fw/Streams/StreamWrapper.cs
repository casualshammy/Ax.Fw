using System;
using System.IO;
using System.Threading.Tasks;

namespace Ax.Fw.Streams;

public class StreamWrapper : Stream
{
  private readonly Stream p_underlyingStream;
  private readonly bool p_disposeUnderlyingStream;
  private readonly long? p_length;
  private readonly long p_initialPosition;
  private long p_relativePosition = 0;

  public StreamWrapper(
    Stream _underlyingStream,
    long? _length,
    bool _disposeUnderlyingStream)
  {
    p_underlyingStream = _underlyingStream;
    p_disposeUnderlyingStream = _disposeUnderlyingStream;
    p_length = _length;
    p_initialPosition = _underlyingStream.Position;
  }

  public override bool CanRead => p_underlyingStream.CanRead;

  public override bool CanSeek => p_underlyingStream.CanSeek;

  public override bool CanWrite => p_underlyingStream.CanWrite;

  public override long Length => p_length ?? (p_underlyingStream.Length - p_initialPosition);

  public override long Position
  {
    get => p_relativePosition;
    set
    {
      if (value < 0)
        throw new InvalidOperationException($"Position < 0");

      var absolutePosition = value + p_initialPosition;

      var length = Length + p_initialPosition;
      if (absolutePosition >= length)
        throw new InvalidOperationException($"Position >= stream length");

      p_relativePosition = value;
      p_underlyingStream.Position = absolutePosition;
    }
  }

  public override void Flush() => p_underlyingStream.Flush();

  public override int Read(byte[] _buffer, int _offset, int _count)
  {
    var length = Length;
    var count = (int)Math.Min(_count, length - p_relativePosition);

    if (count <= 0)
      return 0;

    var bytesRead = p_underlyingStream.Read(_buffer, _offset, count);
    p_relativePosition += bytesRead;
    return bytesRead;
  }

  public override long Seek(long _offset, SeekOrigin _origin)
  {
    var relativePosition = 0L;
    if (_origin == SeekOrigin.Begin)
      relativePosition = _offset;
    else if (_origin == SeekOrigin.Current)
      relativePosition = p_relativePosition + _offset;
    else if (_origin == SeekOrigin.End)
      relativePosition = Length - _offset;

    Position = relativePosition;
    return relativePosition;
  }

  public override void SetLength(long _value) => throw new NotImplementedException();

  public override void Write(byte[] _buffer, int _offset, int _count)
  {
    var length = Length;
    var count = (int)Math.Min(_count, length - p_relativePosition);

    if (count <= 0)
      return;

    p_underlyingStream.Write(_buffer, _offset, count);
    p_relativePosition += _count;
  }

  protected override void Dispose(bool _disposing)
  {
    if (p_disposeUnderlyingStream)
      p_underlyingStream.Dispose();
  }

  public override async ValueTask DisposeAsync()
  {
    if (p_disposeUnderlyingStream)
      await p_underlyingStream.DisposeAsync();
  }

}
