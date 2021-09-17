using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Ax.Fw
{
    /// <summary>
    /// Simple storage for data in JSON files
    /// </summary>
    public class JsonStorage<T> where T : new()
    {
        public string JsonFilePath { get; private set; }

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
        /// Load data from JSON file
        /// </summary>
        /// <typeparam name="T">Type of readed data</typeparam>
        /// <param name="createNewFile">
        /// If TRUE, new file will be created if doesn't exist. If FALSE, method will throw <see
        /// cref="FileNotFoundException"/> if file doesn't exist
        /// </param>
        /// <returns>
        /// Instance of <see cref="T"/>. If new file is created, will return new instance of <see cref="T"/>
        /// </returns>
        public T Load(bool createNewFile = true)
        {
            bool fileExist = File.Exists(JsonFilePath);
            if (!createNewFile && !fileExist)
                throw new FileNotFoundException();
            if (!fileExist)
            {
                T newInstance = new T();
                Save(newInstance);
                return newInstance;
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(JsonFilePath, Encoding.UTF8));
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
