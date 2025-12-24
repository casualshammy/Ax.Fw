using Ax.Fw.App.Data;
using Ax.Fw.Extensions;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using System.Reactive.Concurrency;

namespace Ax.Fw.App.Extensions;

public static class LogExtensions
{
  /// <summary>
  /// Enables console logging for the application by attaching a console log provider to the current <see
  /// cref="AppBase"/> instance.
  /// </summary>
  /// <remarks>This method configures the application's logging to output messages to the console.</remarks>
  /// <param name="_appBase">The <see cref="AppBase"/> instance to configure with console logging.</param>
  /// <returns>The same <see cref="AppBase"/> instance, allowing for fluent configuration.</returns>
  public static AppBase UseConsoleLog(this AppBase _appBase)
  {
    if (_appBase.DependencyManager.Locate<ILog>() is not GenericLog log)
      throw new InvalidOperationException($"Can't get the instance of {typeof(GenericLog)}!");

    log.AttachConsoleLog();
    return _appBase;
  }

  /// <summary>
  /// Configures the application to log entries to a file using a dynamically generated file name.
  /// </summary>
  /// <remarks>This method attaches a file-based logger to the application, enabling log entries to be written
  /// to files whose names are determined at runtime. The file logger is integrated with the application's dependency
  /// management system and supports error handling and notification of written files.</remarks>
  /// <param name="_appBase">The application instance to configure for file-based logging.</param>
  /// <param name="_fileNameFactory">A delegate that returns the file name to use for logging. The function is called to determine the file name for
  /// each log file.</param>
  /// <param name="_onError">An optional callback invoked when an error occurs during file logging. Receives the exception and the log entries
  /// that failed to be written.</param>
  /// <param name="_filesWrittenCallback">An optional callback invoked with the set of file names that have been written to after each logging operation.</param>
  /// <returns>The original <see cref="AppBase"/> instance, allowing for method chaining.</returns>
  public static AppBase UseFileLog(
    this AppBase _appBase,
    Func<string> _fileNameFactory,
    Action<Exception, IEnumerable<LogEntry>>? _onError = null,
    Action<HashSet<string>>? _filesWrittenCallback = null)
  {
    if (_appBase.DependencyManager.Locate<ILog>() is not GenericLog log)
      throw new InvalidOperationException($"Can't get the instance of {typeof(GenericLog)}!");

    log.AttachFileLog(_fileNameFactory, TimeSpan.FromSeconds(1), _onError, _filesWrittenCallback);
    return _appBase;
  }

  /// <summary>
  /// Configures file-based logging for the application using a file path determined from the current configuration.
  /// </summary>
  /// <remarks>This method attaches a file logger to the application's logging system, with the log file path
  /// dynamically generated from the configuration of type <typeparamref name="T"/>. The file path is updated
  /// automatically when the configuration changes. Optionally, you can handle logging errors and receive notifications
  /// when log files are written.</remarks>
  /// <typeparam name="T">The type of the configuration object used to determine the log file path. Must be a reference type.</typeparam>
  /// <param name="_appBase">The application base instance to configure logging for.</param>
  /// <param name="_fileNameFactory">A delegate that generates the log file path based on the current configuration value of type <typeparamref
  /// name="T"/>. The delegate receives the configuration object (which may be <see langword="null"/>) and should return
  /// the desired file path as a string.</param>
  /// <param name="_onError">An optional callback invoked when an exception occurs during file logging. The callback receives the exception and
  /// the collection of log entries that failed to be written.</param>
  /// <param name="_filesWrittenCallback">An optional callback invoked after log entries are successfully written to files. The callback receives a set of
  /// file paths that were written.</param>
  /// <returns>The same <see cref="AppBase"/> instance, allowing for method chaining.</returns>
  public static AppBase UseFileLogFromConf<T>(
    this AppBase _appBase,
    Func<T?, string?> _fileNameFactory,
    Action<Exception, IEnumerable<LogEntry>>? _onError = null,
    Action<HashSet<string>>? _filesWrittenCallback = null)
    where T : class
  {
    var lifetime = _appBase.DependencyManager.Locate<IReadOnlyLifetime>();

    if (_appBase.DependencyManager.Locate<ILog>() is not GenericLog log)
      throw new InvalidOperationException($"Can't get the instance of {typeof(GenericLog)}!");

    var confFlow = _appBase.DependencyManager.Locate<IObservableConfig<T?>>() 
      ?? throw new InvalidOperationException($"Can't get the instance of {typeof(IObservableConfig<T?>)}!");

    var confProp = confFlow.ToNullableProperty(lifetime, null);

    string? factory()
    {
      var conf = confProp.Value;
      var path = _fileNameFactory.Invoke(conf);
      return path;
    }

    log.AttachFileLog(factory, TimeSpan.FromSeconds(1), _onError, _filesWrittenCallback);
    return _appBase;
  }

  /// <summary>
  /// Configures the application to automatically clean up rotated log files in the specified directory according to the
  /// provided file log rotation settings.
  /// </summary>
  /// <remarks>This method registers a background cleanup process that deletes old log files based
  /// on the settings in <paramref name="_desc"/>. The cleanup occurs automatically during the application's lifetime.
  /// Ensure that the directory and file pattern specified in <paramref name="_desc"/> are correct to avoid unintended
  /// file deletions.</remarks>
  /// <param name="_appBase">The application instance to configure for log file rotation cleanup.</param>
  /// <param name="_desc">A <see cref="FileLogRotateDescription"/> object that specifies the directory, file pattern, retention policy, and
  /// other options for log file rotation and cleanup.</param>
  /// <returns>The same <see cref="AppBase"/> instance, enabling method chaining.</returns>
  public static AppBase UseFileLogRotate(
    this AppBase _appBase,
    FileLogRotateDescription _desc)
  {
    _appBase.DependencyManager
      .ActivateOnStart((IReadOnlyLifetime _lifetime) =>
      {
        _lifetime.ToDisposeOnEnding(FileLoggerCleaner.Create(
          _desc.Directory, _desc.Recursive, _desc.LogFilesPattern, _desc.LogFileTtl, _desc.GzipFiles, TimeSpan.FromHours(3)));
      });

    return _appBase;
  }

  /// <summary>
  /// Configures file log rotation for the application using settings provided by a configuration flow and a factory
  /// function.
  /// </summary>
  /// <remarks>This method sets up file log rotation based on configuration changes observed.</remarks>
  /// <typeparam name="T">The type of the configuration object used to produce rotation options.</typeparam>
  /// <param name="_appBase">The application base instance to configure file log rotation for.</param>
  /// <param name="_factory">A factory function that receives the current configuration value and returns a <see
  /// cref="FileLogRotateDescription"/> describing the log rotation settings. If the factory returns <see
  /// langword="null"/>, log rotation is not configured for that configuration value.</param>
  /// <returns>The same <see cref="AppBase"/> instance, enabling method chaining.</returns>
  public static AppBase UseFileLogRotateFromConf<T>(
    this AppBase _appBase,
    Func<T?, FileLogRotateDescription?> _factory)
  {
    _appBase.DependencyManager
      .ActivateOnStart((IObservableConfig<T?> _confFlow, IReadOnlyLifetime _lifetime) =>
      {
        var scheduler = new EventLoopScheduler();

        _confFlow
          .HotAlive(_lifetime, scheduler, (_conf, _life) =>
          {
            var desc = _factory.Invoke(_conf);
            if (desc == null)
              return;

            _life.ToDisposeOnEnding(FileLoggerCleaner.Create(
              desc.Directory, desc.Recursive, desc.LogFilesPattern, desc.LogFileTtl, desc.GzipFiles, TimeSpan.FromHours(3)));
          });
      });

    return _appBase;
  }
}
