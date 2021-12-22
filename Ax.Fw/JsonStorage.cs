#nullable enable
using Ax.Fw.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ax.Fw
{
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
            bool fileExist = File.Exists(JsonFilePath);
            if (!fileExist)
            {
                T newInstance = await _defaultFactory();
                return newInstance;
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(JsonFilePath, Encoding.UTF8)) ?? throw new InvalidCastException($"Can't parse file!");
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
            bool fileExist = File.Exists(JsonFilePath);
            if (!fileExist)
            {
                T newInstance = _defaultFactory();
                return newInstance;
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(JsonFilePath, Encoding.UTF8)) ?? throw new InvalidCastException($"Can't parse file!");
            }
        }

        /// <summary>
        /// Save data to JSON file
        /// </summary>
        /// <param name="data">Data to save</param>
        public void Save(T data, bool humanReadable = false)
        {
            string jsonData = JsonConvert.SerializeObject(data, humanReadable ? Formatting.Indented : Formatting.None);
            File.WriteAllText(JsonFilePath, jsonData, Encoding.UTF8);
        }

    }
}
