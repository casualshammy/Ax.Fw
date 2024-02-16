using Ax.Fw.App;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ax.Fw.Tests.App;

internal record TestConfig(string Path, int Value) : IConfigDefinition
{
  public static string FilePath => "C:\\Windows\\Temp\\ax.fw.app.test-config.json";

  public static JsonSerializerContext? JsonCtx => TestConfigJsonCtx.Default;
}

[JsonSerializable(typeof(TestConfig))]
internal partial class TestConfigJsonCtx : JsonSerializerContext
{

}

public class AppTests
{
  [Fact(Timeout = 30000)]
  public async Task IObservableConfigTestAsync()
  {
    var data = new TestConfig("test", 123);
    var json = JsonSerializer.Serialize(data, TestConfigJsonCtx.Default.TestConfig);
    File.WriteAllText(TestConfig.FilePath, json);

    using var semaphore = new SemaphoreSlim(0, 1);

    var app = AppBase
      .Create()
      .UseConfigFile<TestConfig>()
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
