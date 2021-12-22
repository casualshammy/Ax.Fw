using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ax.Fw.Extensions
{
    public static class DirectoryInfoExtensions
    {
        public static long CalcDirectorySize(this DirectoryInfo _directoryInfo)
        {
            var sum = 0L;
            foreach (FileSystemInfo fsInfo in _directoryInfo.GetFileSystemInfos())
            {
                if (fsInfo is FileInfo fileInfo)
                {
                    sum += fileInfo.Length;
                }
                else if (fsInfo is DirectoryInfo directoryInfo)
                {
                    sum += CalcDirectorySize(directoryInfo);
                }
            }
            return sum;
        }

        public static IEnumerable<string> FindFilesByName(this DirectoryInfo _directoryInfo, string _fileName, int _depth = int.MaxValue)
        {
            if (_depth == 0)
                yield break;

            FileSystemInfo[] infos = null;
            try
            {
                infos = _directoryInfo.GetFileSystemInfos();
            }
            catch { /*we don't care if we can't access some folder*/ }

            if (infos != null)
            {
                foreach (FileSystemInfo fsInfo in infos)
                {
                    if (fsInfo is FileInfo file)
                    {
                        if (file.Name == _fileName)
                        {
                            yield return file.FullName;
                        }
                    }
                    else if (fsInfo is DirectoryInfo directory)
                    {
                        foreach (var p in FindFilesByName(directory, _fileName, _depth - 1))
                            yield return p;
                    }
                }
            }
        }

        /// <summary>
        ///     Finds directories on path
        /// </summary>
        /// <param name="_directoryInfo">Root path (from where search starts)</param>
        /// <param name="_dirName">Name of directory to find</param>
        /// <param name="_depth">How much level of subdirectories to dig in (default value: int.MaxValue)</param>
        /// <returns><see cref="IEnumerable"/> of full paths of finded directories</returns>
        public static IEnumerable<string> FindDirectories(this DirectoryInfo _directoryInfo, string _dirName, int _depth = int.MaxValue)
        {
            if (_depth == 0)
                yield break;

            FileSystemInfo[] infos = null;
            try
            {
                infos = _directoryInfo.GetFileSystemInfos();
            }
            catch { /*we don't care if we can't access some folder*/ }

            if (infos != null)
            {
                foreach (FileSystemInfo fsInfo in infos)
                {
                    if (fsInfo is DirectoryInfo directory)
                    {
                        if (directory.Name == _dirName)
                            yield return directory.FullName;
                        foreach (var p in FindDirectories(directory, _dirName, _depth - 1))
                            yield return p;
                    }
                }
            }
        }

        public static string CreateMd5ForFolder(this DirectoryInfo _directoryInfo)
        {
            List<string> files = Directory.GetFiles(_directoryInfo.FullName, "*.*", SearchOption.AllDirectories).OrderBy(p => p).ToList();
            using (MD5 md5 = MD5.Create())
            {
                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    string relativePath = file.Substring(_directoryInfo.FullName.Length + 1);
                    byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);
                    byte[] contentBytes = File.ReadAllBytes(file);
                    if (i == files.Count - 1)
                        md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                    else
                        md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }
                return BitConverter.ToString(md5.Hash).Replace("-", "");
            }
        }

        public static void DirectoryCopy(this DirectoryInfo _sourceDirectory, string destDirName, bool copySubDirs)
        {
            if (!_sourceDirectory.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + _sourceDirectory.FullName);

            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            FileInfo[] files = _sourceDirectory.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }
            if (copySubDirs)
            {
                DirectoryInfo[] dirs = _sourceDirectory.GetDirectories();
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir, temppath, true);
                }
            }
        }

        public static bool TryDelete(this DirectoryInfo _dir, bool _recursive)
        {
            try
            {
                _dir.Delete(_recursive);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
