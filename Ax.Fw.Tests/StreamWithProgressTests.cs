﻿using Ax.Fw.Extensions;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class StreamWithProgressTests
{
  private readonly ITestOutputHelper p_output;

  public StreamWithProgressTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact]
  public void TestRead()
  {
    var lifetime = new Lifetime();
    try
    {
      var progress = 0d;
      void onProgress(double _progress) => progress = _progress;

      var data = new byte[1024 * 1024];
      Random.Shared.NextBytes(data);
      using (var outputMs = new MemoryStream(data))
      using (var inputMs = new MemoryStream())
      using (var stream = new StreamWithProgress(data.Length, outputMs, onProgress))
        stream.CopyToAsync(inputMs);

      Assert.Equal(100d, progress);
    }
    finally
    {
      lifetime.End();
    }
  }

  [Fact]
  public void TestWrite()
  {
    var lifetime = new Lifetime();
    try
    {
      var progress = 0d;
      void onProgress(double _progress) => progress = _progress;

      var data = new byte[1024 * 1024];
      Random.Shared.NextBytes(data);
      using (var outputMs = new MemoryStream(data))
      using (var inputMs = new MemoryStream())
      using (var stream = new StreamWithProgress(data.Length, inputMs, onProgress))
        outputMs.CopyToAsync(stream);

      Assert.Equal(100d, progress);
    }
    finally
    {
      lifetime.End();
    }
  }

}
