using Microsoft.Data.Sqlite;

namespace Ax.Fw.Storage.Data;

public class BlobStream : Stream
{
  private readonly SqliteConnection p_connection;
  private readonly SqliteBlob p_blob;

  internal BlobStream(
    SqliteConnection _connection,
    SqliteBlob _blob)
  {
    p_connection = _connection;
    p_blob = _blob;
  }

  public override bool CanRead => p_blob.CanRead;

  public override bool CanSeek => p_blob.CanSeek;

  public override bool CanWrite => p_blob.CanWrite;

  public override long Length => p_blob.Length;

  public override long Position { get => p_blob.Position; set => p_blob.Position = value; }

  public override void Flush() => p_blob.Flush();

  public override int Read(byte[] _buffer, int _offset, int _count) => p_blob.Read(_buffer, _offset, _count);

  public override long Seek(long _offset, SeekOrigin _origin) => p_blob.Seek(_offset, _origin);

  public override void SetLength(long _value) => p_blob.SetLength(_value);

  public override void Write(byte[] _buffer, int _offset, int _count) => p_blob.Write(_buffer, _offset, _count);

  public byte[] ToArray()
  {
    using (var ms = new MemoryStream())
    {
      CopyTo(ms);
      return ms.ToArray();
    }
  }

  protected override void Dispose(bool _disposing)
  {
    try
    {
      p_blob?.Dispose();
    }
    catch
    { }

    try
    {
      p_connection?.Dispose();
    }
    catch
    { }
  }
}