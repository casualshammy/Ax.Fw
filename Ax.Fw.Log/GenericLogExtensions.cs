using Ax.Fw.Extensions;
using Ax.Fw.Log.Data;
using Ax.Fw.SharedTypes.Data.Log;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text;

namespace Ax.Fw.Log;

public static class GenericLogExtensions
{
  public static GenericLog AttachFileLog(
    this GenericLog _log,
    Func<string?> _filenameFactory,
    TimeSpan _buffer,
    Action<Exception, IEnumerable<LogEntry>>? _onError = null,
    Action<HashSet<string>>? _filesWrittenCallback = null)
  {
    string getLogEntryString(LogEntry _entry)
    {
      return $"| {_entry.GetTypePrefix()} | {_entry.Time:dd.MM.yyyy HH:mm:ss.fff} || {_entry.Scope} || {_entry.Text}";
    }

    var filesWritten = new HashSet<string>();
    var logEntriesQueue = new ConcurrentQueue<LogEntry>();

    void flush()
    {
      try
      {
        if (!logEntriesQueue.IsEmpty)
        {
          var filepath = _filenameFactory();
          if (filepath == null)
            return;

          var stringBuilder = new StringBuilder();

          while (logEntriesQueue.TryDequeue(out var entry))
            stringBuilder.AppendLine(getLogEntryString(entry));

          File.AppendAllText(filepath, stringBuilder.ToString(), Encoding.UTF8);

          filesWritten.Add(filepath);
          _filesWrittenCallback?.Invoke(filesWritten);

          stringBuilder.Clear();
          stringBuilder = null;
        }
      }
      catch (Exception ex)
      {
        _onError?.Invoke(ex, logEntriesQueue.Select(_ => _));
      }
    }

    _log.AddEndAction(() => flush());

    var queueSubs = _log.LogEntries
      .Subscribe(_ => logEntriesQueue.Enqueue(_));
    _log.AddEndAction(() => queueSubs.Dispose());

    var writerSubs = Observable
      .Interval(_buffer)
      .Subscribe(_ => flush());
    _log.AddEndAction(() => writerSubs.Dispose());

    return _log;
  }

  public static GenericLog AttachConsoleLog(
    this GenericLog _log)
  {
    void consoleWriteColourText(string _text, ConsoleColor _colour)
    {
      Console.ForegroundColor = _colour;
      Console.Write(_text);
      Console.ResetColor();
    }

    IEnumerable<LogTextPart> parseLogEntryMsg(string _msg)
    {
      var startIndex = 0;
      var endIndex = 0;
      var currentMode = LogTextSpecialContentType.None;

      var msgLength = _msg.Length;
      for (var i = 0; i < msgLength; i++)
      {
        var c = _msg[i];
        if (c != '*' && c != '_')
        {
          endIndex++;
          continue;
        }

        if (i + 1 >= msgLength)
          break;

        if (_msg[i + 1] != c)
        {
          endIndex++;
          continue;
        }

        var specialModeActive = currentMode != LogTextSpecialContentType.None;

        currentMode = c switch
        {
          '*' => LogTextSpecialContentType.Green,
          '_' => LogTextSpecialContentType.Cyan,
          _ => LogTextSpecialContentType.None
        };

        if (!specialModeActive)
        {
          yield return new LogTextPart(LogTextSpecialContentType.None, _msg[startIndex..endIndex]);
        }
        else
        {
          yield return new LogTextPart(currentMode, _msg[startIndex..endIndex]);
          currentMode = LogTextSpecialContentType.None;
        }

        i++;
        startIndex = endIndex = i + 1;
      }

      if (startIndex < msgLength)
        yield return new LogTextPart(LogTextSpecialContentType.None, _msg[startIndex..]);
    }

    void writeInConsole(LogEntry _entry)
    {
      consoleWriteColourText("| ", ConsoleColor.Gray);

      if (_entry.Type == LogEntryType.INFO)
        consoleWriteColourText(_entry.GetTypePrefix(), ConsoleColor.Green);
      else if (_entry.Type == LogEntryType.WARN)
        consoleWriteColourText(_entry.GetTypePrefix(), ConsoleColor.Yellow);
      else if (_entry.Type == LogEntryType.ERROR)
        consoleWriteColourText(_entry.GetTypePrefix(), ConsoleColor.Red);

      consoleWriteColourText($" | {_entry.Time:dd.MM.yyyy HH:mm:ss.fff} | ", ConsoleColor.Gray);

      consoleWriteColourText(_entry.Scope ?? "", ConsoleColor.White);

      consoleWriteColourText(" || ", ConsoleColor.Gray);

      if (_entry.Type == LogEntryType.INFO)
      {
        foreach (var textPart in parseLogEntryMsg(_entry.Text))
        {
          if (textPart.Type == LogTextSpecialContentType.None)
            consoleWriteColourText(textPart.Text, ConsoleColor.White);
          else if (textPart.Type == LogTextSpecialContentType.Green)
            consoleWriteColourText(textPart.Text, ConsoleColor.Green);
          else if (textPart.Type == LogTextSpecialContentType.Cyan)
            consoleWriteColourText(textPart.Text, ConsoleColor.Cyan);
        }
      }
      else if (_entry.Type == LogEntryType.WARN)
      {
        consoleWriteColourText(_entry.Text, ConsoleColor.Yellow);
      }
      else if (_entry.Type == LogEntryType.ERROR)
      {
        consoleWriteColourText(_entry.Text, ConsoleColor.Red);
      }

      Console.WriteLine();
      Console.Out.Flush();
    }

    var consoleLock = new object();

    var writerSubs = _log.LogEntries
      .Subscribe(_ =>
      {
        lock (consoleLock)
          writeInConsole(_);
      });
    _log.AddEndAction(() => writerSubs.Dispose());

    return _log;
  }

}