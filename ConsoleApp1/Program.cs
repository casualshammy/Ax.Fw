using Ax.Fw.App;
using Ax.Fw.App.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Storage;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ConsoleApp1;

internal class Program
{
  static async Task Main(string[] args)
  {
    Console.WriteLine($"index.html -> {Ax.Fw.MimeTypes.GetMimeByExtension("index.html")}");
    Console.WriteLine($"index.js -> {Ax.Fw.MimeTypes.GetMimeByExtension("index.js")}");
    Console.WriteLine($"index.jpeg -> {Ax.Fw.MimeTypes.GetMimeByExtension("index.jpeg")}");
    Console.WriteLine($"index.jpg -> {Ax.Fw.MimeTypes.GetMimeByExtension("index.jpg")}");
    Console.WriteLine($"index.css -> {Ax.Fw.MimeTypes.GetMimeByExtension("index.css")}");
    Console.WriteLine($".opus -> {Ax.Fw.MimeTypes.GetMimeByExtension(".opus")}");

    return;
    using var fileStream = File.Open("E:\\repos\\private\\Ax.Fw\\Ax.Fw\\MimeTypes.csold", FileMode.Open, FileAccess.Read);
    using var resFileStream = File.Open("E:\\repos\\private\\Ax.Fw\\Ax.Fw\\MimeTypes.csnew", FileMode.Create, FileAccess.Write);

    using var reader = new StreamReader(fileStream);
    using var writer = new StreamWriter(resFileStream);

    var regex = new Regex(@"new\(""([^""]+?)""\)");

    while (!reader.EndOfStream)
    {
      var line = await reader.ReadLineAsync();
      if (line == null)
        break;

      var match = regex.Match(line);
      if (!match.Success)
      {
        await writer.WriteLineAsync(line);
        continue;
      }

      var mime = match.Groups[1].Value;
      var ext = MimeMapping.MimeUtility.GetExtensions(mime);
      if (ext != null && ext.Length > 0)
      {
        line = line.Replace(match.Value, $"new(new string[] {{\"{string.Join("\", \"", ext)}\"}}, \"{mime}\")");
        await writer.WriteLineAsync(line);
        Console.WriteLine($"{mime} => {ext}");
        continue;
      }

      await writer.WriteLineAsync(line);
    }

    return;
    var docStorage = new SqliteDocumentStorageV2(Path.GetTempFileName(), ProgramJsonCtx.Default);
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