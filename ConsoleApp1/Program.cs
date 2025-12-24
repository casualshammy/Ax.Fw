using Ax.Fw.App;
using Ax.Fw.App.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Storage;
using System.Reactive.Linq;
using System.Text.Json.Serialization;

namespace ConsoleApp1;

internal class Program
{
  static async Task Main(string[] args)
  {
    var docStorage = new SqliteDocumentStorage(Path.GetTempFileName(), ProgramJsonCtx.Default);
    docStorage.WriteDocument("test-ns", "test-key", "test-data");
    var doc = docStorage.ReadDocument<string>("test-ns", "test-key");
    Console.WriteLine($"{doc?.Namespace}/{doc?.Key} = {doc?.Data}");

    Console.WriteLine($"Starting AppBase...");

    await AppBase
      .Create()
      .UseConsoleLog()
      .AddSingleton<string>(_ctx => _ctx.CreateInstance((IReadOnlyLifetime _lifetime, ILog _log) =>
      {
        _log.Info($"Hello there!");
        _log.Info($"**Hello** there!");
        _log.Info($"Hello **there**!");
        _log.Info($"__Hello__ there!");
        _log.Info($"Hello __there__!");
        _log.Info($"Hel__lo the__re!");
        _log.Info($"**Hel**lo the**re**!");
        _log.Info($"**Hel**lo the__re__!");
        _log["scope"].Warn($"**Hello** there!");
        _log["scope/subscope"].Error($"Hello __there__!");

        _log.Info($"**String** is __constructed__");
        return $"{_lifetime.IsCancellationRequested}";
      }))
      .ActivateOnStart<string>()
      .RunWaitAsync();

    Console.WriteLine($"AppBase is finished");
  }

  private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";
}

[JsonSerializable(typeof(string))]
internal partial class ProgramJsonCtx : JsonSerializerContext { }