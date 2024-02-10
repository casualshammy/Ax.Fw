using Ax.Fw.App;
using Ax.Fw.App.Attributes;
using Ax.Fw.App.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests.App;

[AppConfigFile(AppTests.TEST_FILE_PATH)]
internal record TestConfig(string Path, int Value);

[JsonSerializable(typeof(TestConfig))]
internal partial class TestConfigJsonCtx : JsonSerializerContext
{

}

public class AppTests
{
  internal const string TEST_FILE_PATH = "C:\\Windows\\Temp\\ax.fw.app.test-config.json";

  [Fact(Timeout = 30000)]
  public async Task IObservableConfigTestAsync()
  {
    var data = new TestConfig("test", 123);
    var json = JsonSerializer.Serialize(data, TestConfigJsonCtx.Default.TestConfig);
    File.WriteAllText(TEST_FILE_PATH, json);

    using var semaphore = new SemaphoreSlim(0, 1);

    var app = AppBase
      .Create()
      .UseConfigFile<TestConfig>(TestConfigJsonCtx.Default)
      .AddSingleton<string>(_ctx => _ctx.CreateInstance((IObservableConfig<TestConfig> _conf) =>
      {
        _conf.Subscribe(_conf =>
        {
          Assert.Equal(data.Path, _conf.Path);
          Assert.Equal(data.Value, _conf.Value);

          semaphore.Release();
        });

        return "";
      }))
      .ActivateOnStart<string>()
      .RunWaitAsync();

    if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(30)))
     Assert.Fail($"Data is not reached the observer");
  }
}
