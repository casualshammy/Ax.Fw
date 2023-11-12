using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ax.Fw.JsonStorages;

/// <summary>
/// Simple storage for data in JSON files
/// </summary>
public class JsonStorageV2<T> : IJsonStorage<T>
{
  /// <summary>
  ///
  /// </summary>
  /// <param name="jsonFilePath">Path to JSON file. Can't be null or empty.</param>
  public JsonStorageV2(string jsonFilePath)
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
    {
      return await _defaultFactory();
    }
    else
    {
      using (var fileStream = File.OpenRead(JsonFilePath))
        return await JsonSerializer.DeserializeAsync<T>(fileStream) ?? await _defaultFactory();
    }
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
    {
      using (var fileStream = File.OpenRead(JsonFilePath))
        return JsonSerializer.Deserialize<T>(fileStream) ?? _defaultFactory();
    }
  }

  /// <summary>
  /// Save data to JSON file
  /// </summary>
  /// <param name="_data">Data to save</param>
  public void Save(T _data, bool _humanReadable = false)
  {
    var serializerOptions = new JsonSerializerOptions { WriteIndented = _humanReadable };

    var jsonData = JsonSerializer.Serialize(_data, serializerOptions);
    File.WriteAllText(JsonFilePath, jsonData, Encoding.UTF8);
  }

}
