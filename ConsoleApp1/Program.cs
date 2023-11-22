using Ax.Fw;
using Ax.Fw.Extensions;
using Ax.Fw.Storage;
using System.Text.Json.Serialization;

namespace ConsoleApp1
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      var lifetime = new Lifetime();
      var dbFile = GetDbTmpPath();
      var counter = 0;
      Console.WriteLine(dbFile);
      Console.WriteLine(counter++);
      try
      {
        var storage = lifetime.ToDisposeOnEnding(new SqliteDocumentStorageAot(dbFile));
        Console.WriteLine(counter++);

        var data = new Data("temp name", new Dictionary<string, int>
        {
          { "key", 123 }
        });
        Console.WriteLine(counter++);

        await storage.WriteSimpleDocumentAsync(123, data, JsonSerializationContext.Default.Data, lifetime.Token);
        Console.WriteLine(counter++);

        var doc = await storage.ReadSimpleDocumentAsync(123, JsonSerializationContext.Default.Data, lifetime.Token);
        Console.WriteLine(counter++);

        Console.WriteLine(doc?.Data.Name);
        Console.WriteLine(doc?.Data.Meta.Count);
      }
      finally
      {
        lifetime.End();
        Console.WriteLine(counter++);
        new FileInfo(dbFile).TryDelete();
        Console.WriteLine(counter++);
      }
    }

    private static string GetDbTmpPath() => $"{Path.GetTempFileName()}";
  }

  internal record Data(string Name, IReadOnlyDictionary<string, int> Meta);

  [JsonSourceGenerationOptions()]
  [JsonSerializable(typeof(Data))]
  internal partial class JsonSerializationContext : JsonSerializerContext
  {
  }
}
