using Ax.Fw.SharedTypes.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ax.Fw.JsonStorages;

/// <summary>
/// Simple storage for data in JSON files
/// </summary>
public class JsonStorage<T> : IJsonStorage<T>
{
  /// <summary>
  ///
  /// </summary>
  /// <param name="jsonFilePath">Path to JSON file. Can't be null or empty.</param>
  public JsonStorage(string jsonFilePath)
  {
    if (string.IsNullOrWhiteSpace(jsonFilePath))
      throw new ArgumentNullException(nameof(jsonFilePath));

    JsonFilePath = jsonFilePath;
  }

  /// <summary>
  /// Path to file
  /// </summary>
  public string JsonFilePath { get; }

  /// <summary>
  /// Loads data from JSON file
  /// </summary>
  /// <param name="_defaultFactory">
  /// If file doesn't exist, this method will be invoked to produce default value
  /// </param>
  /// <returns>
  /// Instance of <see cref="T"/>
  /// </returns>
  public async Task<T> LoadAsync(Func<Task<T>> _defaultFactory)
  {
    if (!File.Exists(JsonFilePath))
      return await _defaultFactory();
    else
      return JsonConvert.DeserializeObject<T>(File.ReadAllText(JsonFilePath, Encoding.UTF8)) ?? await _defaultFactory();
  }

  /// <summary>
  /// Loads data from JSON file
  /// </summary>
  /// <param name="_defaultFactory">
  /// If file doesn't exist, this method will be invoked to produce default value
  /// </param>
  /// <returns>
  /// Instance of <see cref="T"/>
  /// </returns>
  public T Load(Func<T> _defaultFactory)
  {
    if (!File.Exists(JsonFilePath))
      return _defaultFactory();
    else
      return JsonConvert.DeserializeObject<T>(File.ReadAllText(JsonFilePath, Encoding.UTF8)) ?? _defaultFactory();
  }

  /// <summary>
  /// Save data to JSON file
  /// </summary>
  /// <param name="_data">Data to save</param>
  public void Save(T _data, bool _humanReadable = false)
  {
    var jsonData = JsonConvert.SerializeObject(_data, _humanReadable ? Formatting.Indented : Formatting.None);
    File.WriteAllText(JsonFilePath, jsonData, Encoding.UTF8);
  }

}
