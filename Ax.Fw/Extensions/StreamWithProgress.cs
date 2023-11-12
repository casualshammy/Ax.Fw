using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Extensions
{
    public class StreamWithProgress : Stream
    {
        private readonly long p_length;
        private readonly Stream p_underlyingStream;
        private readonly Action<double>? p_progress;

        private long p_position = 0;

        public StreamWithProgress(long _length, Stream _underlyingStream, Action<double>? _progress)
        {
            if (_length == 0)
                throw new ArgumentException($"Must be > 0", nameof(_length));

            p_length = _length;
            p_underlyingStream = _underlyingStream;
            p_progress = _progress;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => p_length;

        public override long Position { get => p_position; set => throw new NotImplementedException(); }

        public override void Flush() { }

        public override int Read(byte[] _buffer, int _offset, int _count)
        {
            if (p_position >= p_length)
                return 0;
            if (!p_underlyingStream.CanRead)
                throw new NotSupportedException();

            int bytesRead = p_underlyingStream.Read(_buffer, _offset, _count);
            p_position += bytesRead;
            p_progress?.Invoke(p_position / (double)p_length * 100);
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] _buffer, int _offset, int _count, CancellationToken _token)
        {
            if (p_position >= p_length)
                return 0;
            if (!p_underlyingStream.CanRead)
                throw new NotSupportedException();

            int bytesRead = await p_underlyingStream.ReadAsync(_buffer, _offset, _count, _token);
            p_position += bytesRead;
            p_progress?.Invoke(p_position / (double)p_length * 100);
            return bytesRead;
        }

        public override long Seek(long _offset, SeekOrigin _origin) => throw new NotImplementedException();

        public override void SetLength(long _value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] _buffer, int _offset, int _count)
        {
            if (p_position >= p_length)
                return;
            if (!p_underlyingStream.CanWrite)
                throw new NotSupportedException();

            p_underlyingStream.Write(_buffer, _offset, _count);
            p_position += _count;
            p_progress?.Invoke(p_position / (double)p_length * 100);
        }

        public override async Task WriteAsync(byte[] _buffer, int _offset, int _count, CancellationToken _ct)
        {
            if (p_position >= p_length)
                return;
            if (!p_underlyingStream.CanWrite)
                throw new NotSupportedException();

            await p_underlyingStream.WriteAsync(_buffer, _offset, _count, _ct);
            p_position += _count;
            p_progress?.Invoke(p_position / (double)p_length * 100);
        }

    }
}
