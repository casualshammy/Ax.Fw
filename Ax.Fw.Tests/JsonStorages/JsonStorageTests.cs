#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.JsonStorages;
using Ax.Fw.SharedTypes.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class JsonStorageTests
{
  record Sample(int Number, string Message);

  private readonly ITestOutputHelper p_output;

  public JsonStorageTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 30000)]
  public async Task BasicJsonStorageTestAsync()
  {
    var lifetime = new Lifetime();
    var tempFile = Path.GetTempFileName();

    var jsonStorage = new JsonStorage<Sample>(tempFile, null, lifetime);
    var result = jsonStorage.Read(() => new Sample(0, string.Empty));
    Assert.Equal(0, result.Number);
    Assert.Equal(string.Empty, result.Message);

    var data = new Sample(123, "456");
    await jsonStorage.WriteAsync(data, lifetime.Token);
    result = jsonStorage.Read(() => new Sample(0, string.Empty));
    Assert.Equal(data.Number, result.Number);
    Assert.Equal(data.Message, result.Message);
  }

  [Fact(Timeout = 30000)]
  public async Task JsonStorageObservableTestAsync()
  {
    var lifetime = new Lifetime();
    var tempFile = Path.GetTempFileName();

    var dataCounter = 0;
    var emptyDataCounter = 0;
    var nullDataCounter = 0;

    var data = new Sample(123, "456");
    var emptyData = new Sample(0, string.Empty);
    Sample? nullData = null;

    var jsonStorage = new JsonStorage<Sample>(tempFile, null, lifetime);
    jsonStorage.Subscribe(_v =>
    {
      if (_v == data)
        ++dataCounter;
      else if (_v == emptyData)
        ++emptyDataCounter;
      else if (_v == null)
        ++nullDataCounter;
    }, lifetime);

    await jsonStorage.WriteAsync(data, lifetime.Token);
    await Task.Delay(3000);

    await jsonStorage.WriteAsync(emptyData, lifetime.Token);
    await Task.Delay(3000);

    await jsonStorage.WriteAsync(nullData, lifetime.Token);
    await Task.Delay(3000);

    Assert.Equal(1, dataCounter);
    Assert.Equal(1, emptyDataCounter);
    Assert.InRange(nullDataCounter, 1, 2);
  }


}
