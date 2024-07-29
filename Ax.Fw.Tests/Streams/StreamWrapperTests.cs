using Ax.Fw.Streams;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests.Streams;

public class StreamWrapperTests
{
  private readonly ITestOutputHelper p_output;

  public StreamWrapperTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 10000)]
  public async Task PositionAndLengthAndDataAsync()
  {
    using var lifetime = new Lifetime();

    var data = new byte[1024];
    Random.Shared.NextBytes(data);
    using var ms = new MemoryStream();
    await ms.WriteAsync(data, lifetime.Token);
    ms.Position = 0;

    {
      ms.Position = 0;
      var wrapper = new StreamWrapper(ms, null, false);
      Assert.Equal(0, wrapper.Position);
      Assert.Equal(data.Length, wrapper.Length);

      Memory<byte> result = new byte[data.Length];
      for (var i = 0; i < 4; i++)
        await wrapper.ReadExactlyAsync(result[(i * 256)..((i + 1) * 256)], lifetime.Token);

      Assert.Equal(data, result);
    }

    {
      ms.Position = data.Length / 2;
      var length = data.Length / 2;
      var wrapper = new StreamWrapper(ms, null, false);
      Assert.Equal(0, wrapper.Position);
      Assert.Equal(length, wrapper.Length);

      Memory<byte> result = new byte[length];
      for (var i = 0; i < 2; i++)
        await wrapper.ReadExactlyAsync(result[(i * 256)..((i + 1) * 256)], lifetime.Token);

      Assert.Equal(data[length..], result);
    }

    {
      ms.Position = 0;
      var length = data.Length / 2;
      var wrapper = new StreamWrapper(ms, length, false);
      Assert.Equal(0, wrapper.Position);
      Assert.Equal(length, wrapper.Length);

      Memory<byte> result = new byte[length];
      for (var i = 0; i < 2; i++)
        await wrapper.ReadExactlyAsync(result[(i * 256)..((i + 1) * 256)], lifetime.Token);
      Assert.Equal(data[..length], result);
    }

    {
      ms.Position = data.Length / 2;
      var length = data.Length / 2;
      var wrapper = new StreamWrapper(ms, length, false);
      Assert.Equal(0, wrapper.Position);
      Assert.Equal(length, wrapper.Length);

      Memory<byte> result = new byte[length];
      for (var i = 0; i < 2; i++)
        await wrapper.ReadExactlyAsync(result[(i * 256)..((i + 1) * 256)], lifetime.Token);
      Assert.Equal(data[^length..], result);
    }

  }

  [Fact(Timeout = 10000)]
  public async Task UnderlyingStreamDisposeAsync()
  {
    using var lifetime = new Lifetime();

    {
      using var ms = GetStream();
      var wrapper = new StreamWrapper(ms, null, false);
      wrapper.Dispose();
      Assert.Equal(5, wrapper.Seek(5, SeekOrigin.Begin));

      await wrapper.DisposeAsync();
      Assert.Equal(5, wrapper.Seek(5, SeekOrigin.Begin));
    }

    {
      using var ms = GetStream();
      var wrapper = new StreamWrapper(ms, null, true);
      wrapper.Dispose();
      Assert.Throws<ObjectDisposedException>(() => wrapper.Seek(5, SeekOrigin.Begin));
    }

    {
      using var ms = GetStream();
      var wrapper = new StreamWrapper(ms, null, true);
      await wrapper.DisposeAsync();
      Assert.Throws<ObjectDisposedException>(() => wrapper.Seek(5, SeekOrigin.Begin));
    }
  }

  [Fact(Timeout = 10000)]
  public async Task SeekAsync()
  {
    using var lifetime = new Lifetime();

    var data = new byte[1024];
    Random.Shared.NextBytes(data);
    using var ms = new MemoryStream();
    await ms.WriteAsync(data, lifetime.Token);
    ms.Position = 0;

    {
      ms.Position = data.Length / 2;
      var length = data.Length / 2;
      var wrapper = new StreamWrapper(ms, length, false);

      wrapper.Seek(32, SeekOrigin.Begin);
      var result = new byte[length - 32];
      await wrapper.ReadExactlyAsync(result, lifetime.Token);
      Assert.Equal(data[(length + 32)..], result);
    }

    {
      ms.Position = data.Length / 2;
      var length = data.Length / 2;
      var wrapper = new StreamWrapper(ms, length, false);

      wrapper.Seek(32, SeekOrigin.Begin);
      wrapper.Seek(32, SeekOrigin.Current);
      var result = new byte[length - 64];
      await wrapper.ReadExactlyAsync(result, lifetime.Token);
      Assert.Equal(data[(length + 64)..], result);
    }

    {
      ms.Position = data.Length / 2;
      var length = data.Length / 2;
      var wrapper = new StreamWrapper(ms, length, false);

      wrapper.Seek(32, SeekOrigin.End);
      var result = new byte[32];
      await wrapper.ReadExactlyAsync(result, lifetime.Token);
      Assert.Equal(data[^32..], result);
    }


  }

  private static MemoryStream GetStream()
  {
    var data = new byte[1024];
    Random.Shared.NextBytes(data);
    var ms = new MemoryStream();
    ms.Write(data);
    ms.Position = 0;
    return ms;
  }

}
