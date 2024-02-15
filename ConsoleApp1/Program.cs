using Ax.Fw;
using Ax.Fw.App;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Storage;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleApp1
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      Console.WriteLine($"Starting AppBase...");

      await AppBase
        .Create()
        .AddSingleton<string>(_ctx => _ctx.CreateInstance((IReadOnlyLifetime _lifetime) =>
        {
          Console.WriteLine($"String is constructed");
          return $"{_lifetime.IsCancellationRequested}";
        }))
        .ActivateOnStart<string>()
        .RunWaitAsync();

      Console.WriteLine($"AppBase is finished");
    }

    private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";
  }

}

