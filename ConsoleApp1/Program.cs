using Ax.Fw;
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
      using var lifetime = new Lifetime();

      IRxProperty<string> p = Observable
        .Return("")
        .ToProperty(lifetime);
    }

    private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";
  }

}

