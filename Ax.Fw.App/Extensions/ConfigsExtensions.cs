using Ax.Fw.App.Data;
using Ax.Fw.DependencyInjection;
using Ax.Fw.JsonStorages;
using Ax.Fw.SharedTypes.Interfaces;
using System.Reactive.Linq;
using System.Text.Json.Serialization;

namespace Ax.Fw.App.Extensions;

public static class ConfigsExtensions
{
  /// <summary>
  /// Registers a configuration source that loads and observes changes from a JSON file, transforming the raw
  /// deserialized data into a user-defined configuration type.
  /// </summary>
  /// <remarks>This method registers an <see cref="IObservableConfig{TOut}"/> service in the application's
  /// dependency container. The configuration is automatically reloaded and updated when the underlying JSON file
  /// changes. If deserialization fails, a warning is logged (if an <see cref="ILog"/> service is available), and the
  /// transformation function is invoked with <see langword="null"/> for the raw data.</remarks>
  /// <typeparam name="TRaw">The type representing the raw data structure as deserialized from the JSON file. Must be a reference type.</typeparam>
  /// <typeparam name="TOut">The type representing the transformed configuration object to be provided to consumers. Must be a reference type.</typeparam>
  /// <param name="_filePath">The path to the JSON configuration file to load and monitor for changes. Cannot be null or empty.</param>
  /// <param name="_jsonCtx">The <see cref="JsonSerializerContext"/> used for deserializing the JSON file. Cannot be null.</param>
  /// <param name="_transform">A transformation function that converts the deserialized <typeparamref name="TRaw"/> object (or <see
  /// langword="null"/> if deserialization fails) and the application dependency context into a <typeparamref
  /// name="TOut"/> configuration object.</param>
  /// <returns>The current <see cref="AppBase"/> instance, enabling method chaining.</returns>
  public static AppBase UseConfigFile<TRaw, TOut>(
    this AppBase _appBase,
    string _filePath,
    JsonSerializerContext _jsonCtx,
    Func<IAppDependencyCtx, TRaw?, TOut?> _transform)
    where TRaw : class
    where TOut : class
  {
    _appBase.AddSingleton(_ctx =>
    {
      return _ctx.CreateInstance<IReadOnlyLifetime, IObservableConfig<TOut?>>((IReadOnlyLifetime _lifetime) =>
      {
        void tryLogDeserializationError(Exception _ex)
        {
          var log = _ctx.LocateOrDefault<ILog>();
          log?.Warn($"Can't parse config file '{_filePath}': {_ex.Message}");
        }

        var observable = new JsonStorage<TRaw>(_filePath, _jsonCtx, _lifetime, tryLogDeserializationError)
          .Select(_ => _transform(_ctx, _));

        return new ObservableConfig<TOut>(observable);
      });
    });

    return _appBase;
  }

  /// <summary>
  /// Registers a configuration source that loads and observes changes from a JSON file.
  /// </summary>
  /// <typeparam name="T">The type representing the configuration data to load from the file. Must be a reference type.</typeparam>
  /// <param name="_filePath">The path to the JSON configuration file. The file must exist and be accessible for reading.</param>
  /// <param name="_jsonCtx">The <see cref="JsonSerializerContext"/> to use for deserializing the configuration file. Cannot be <see
  /// langword="null"/>.</param>
  /// <returns>The current <see cref="AppBase"/> instance, enabling method chaining.</returns>
  public static AppBase UseConfigFile<T>(
    this AppBase _appBase,
    string _filePath, 
    JsonSerializerContext _jsonCtx)
    where T : class
  {
    return _appBase.UseConfigFile<T, T>(_filePath, _jsonCtx, (_, _raw) => _raw);
  }

  /// <summary>
  /// Registers a configuration source that loads and observes changes from a JSON file.
  /// </summary>
  /// <remarks>This method retrieves the configuration file path and JSON context from the specified
  /// configuration definition type <typeparamref name="T"/>.</remarks>
  /// <typeparam name="T">The type that defines the configuration file to use. Must implement <see cref="IConfigDefinition"/>.</typeparam>
  /// <returns>The current <see cref="AppBase"/> instance, enabling method chaining.</returns>
  public static AppBase UseConfigFile<T>(
    this AppBase _appBase)
    where T : class, IConfigDefinition
  {
    var filePath = T.FilePath;
    var jsonCtx = T.JsonCtx;

    return _appBase.UseConfigFile<T>(filePath, jsonCtx);
  }
}
