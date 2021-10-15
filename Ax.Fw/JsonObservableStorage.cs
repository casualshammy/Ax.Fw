﻿#nullable enable
using Ax.Fw.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text;

namespace Ax.Fw
{
    /// <summary>
    /// Simple storage for data in JSON files
    /// </summary>
    public class JsonObservableStorage<T> : JsonStorage<T>, IJsonObservableStorage<T>
    {
        private readonly FileSystemWatcher p_watcher;
        private readonly ReplaySubject<T?> p_changesFlow = new(1);

        /// <summary>
        ///
        /// </summary>
        /// <param name="jsonFilePath">Path to JSON file. Can't be null or empty.</param>
        public JsonObservableStorage(ILifetime _lifetime, string jsonFilePath) : base(jsonFilePath)
        {
            var directory = Path.GetDirectoryName(jsonFilePath);
            var filename = Path.GetFileName(jsonFilePath);

            _lifetime.DisposeOnCompleted(p_changesFlow);

            _lifetime.DisposeOnCompleted(p_watcher = new FileSystemWatcher(directory, filename)
            {
                NotifyFilter = NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size
            });
            p_watcher.IncludeSubdirectories = false;
            p_watcher.EnableRaisingEvents = true;
            p_watcher.Created += WatcherFile_Created;
            p_watcher.Changed += WatcherFile_Changed;
            p_watcher.Deleted += WatcherFile_Deleted;
            _lifetime.DoOnCompleted(() =>
            {
                p_watcher.Created -= WatcherFile_Created;
                p_watcher.Changed -= WatcherFile_Changed;
                p_watcher.Deleted -= WatcherFile_Deleted;
            });
        }

        /// <summary>
        /// IObservable of changes in data
        /// </summary>
        public IObservable<T?> Changes => p_changesFlow;

        private T? GetDataOrDefault()
        {
            bool fileExist = File.Exists(JsonFilePath);
            if (!fileExist)
                return default;

            return JsonConvert.DeserializeObject<T>(File.ReadAllText(JsonFilePath, Encoding.UTF8)) ?? default;
        }

        private void WatcherFile_Created(object sender, FileSystemEventArgs e)
        {
            p_changesFlow.OnNext(GetDataOrDefault());
        }

        private void WatcherFile_Changed(object sender, FileSystemEventArgs e)
        {
            p_changesFlow.OnNext(GetDataOrDefault());
        }

        private void WatcherFile_Deleted(object sender, FileSystemEventArgs e)
        {
            p_changesFlow.OnNext(default);
        }

    }
}