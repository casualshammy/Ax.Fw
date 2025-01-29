using Ax.Fw.Streams;
using System;
using System.IO;

namespace Ax.Fw.Extensions;

public static class StreamExtensions
{
  public static StreamWithProgressCallback WithProgress(this Stream _stream, long _streamLength, Action<double> _onProgressChanged)
  {
    return new StreamWithProgressCallback(_streamLength, _stream, _ => _onProgressChanged(_ / 100));
  }

}
