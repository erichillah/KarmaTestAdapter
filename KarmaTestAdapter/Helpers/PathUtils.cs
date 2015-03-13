﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using Ude;

namespace KarmaTestAdapter.Helpers
{
    public static class PathUtils
    {
        public static string GetFullPath(string path, string basePath)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return path.Replace('/', Path.DirectorySeparatorChar);
            }
            var oldCwd = System.IO.Directory.GetCurrentDirectory();
            try
            {
                System.IO.Directory.SetCurrentDirectory(basePath);
                return Path.GetFullPath(path);
            }
            finally
            {
                System.IO.Directory.SetCurrentDirectory(oldCwd);
            }
        }

        public static bool PathsEqual(string path1, string path2)
        {
            if (string.IsNullOrWhiteSpace(path1) || string.IsNullOrWhiteSpace(path2))
            {
                return false;
            }
            return string.Equals(Path.GetFullPath(path1), Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);
        }

        public static bool PathHasFileName(string path, string fileName)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }
            return string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSettingsFile(string path)
        {
            return PathHasFileName(path, Globals.SettingsFilename);
        }

        public static bool IsKarmaConfigFile(string path)
        {
            return PathHasFileName(path, Globals.KarmaConfigFilename);
        }

        public static bool IsInDirectory(string path, string directory)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }
            return string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetRelativePath(string basePath, string fullPath, bool onlyLocal = true)
        {
            if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(fullPath))
            {
                return fullPath ?? "";
            }

            if (Directory.Exists(basePath) && !basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            var relativePath = new Uri(basePath)
                .MakeRelativeUri(new Uri(fullPath))
                .ToString()
                .Replace('/', Path.DirectorySeparatorChar);

            if (onlyLocal && relativePath.StartsWith("."))
            {
                return fullPath;
            }

            return relativePath;
        }

        public static string GetPhysicalPath(string path)
        {
            path = Path.GetFullPath(path);
            if (File.Exists(path))
            {
                return GetPhysicalPath(new FileInfo(path));
            }
            else if (Directory.Exists(path))
            {
                return GetPhysicalPath(new DirectoryInfo(path));
            }
            else
            {
                return path;
            }
        }

        private static string GetPhysicalPath(FileSystemInfo info)
        {
            if (info is FileInfo)
            {
                return GetPhysicalPath(info as FileInfo);
            }
            else if (info is DirectoryInfo)
            {
                return GetPhysicalPath(info as DirectoryInfo);
            }
            else
            {
                throw new Exception(string.Format("Unknown FileSystemInfo: {0}", info));
            }
        }

        private static string GetPhysicalPath(FileInfo info)
        {
            var name = info.Directory.EnumerateFiles(info.Name).First().Name;
            return Path.Combine(GetPhysicalPath(info.Directory), name);
        }

        private static string GetPhysicalPath(DirectoryInfo info)
        {
            if (info.Parent != null)
            {
                var name = info.Parent.EnumerateDirectories(info.Name).First().Name;
                return Path.Combine(GetPhysicalPath(info.Parent), name);
            }
            else
            {
                return info.Name;
            }
        }

        public static Stream OpenRead(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public static string ReadFileText(string path, Encoding defaultEncoding = null)
        {
            using (var fs = OpenRead(path))
            using (var reader = new StreamReader(fs, DetectEncoding(fs, defaultEncoding)))
            {
                return reader.ReadToEnd();
            }
        }

        private static Encoding DetectEncoding(Stream stream, Encoding defaultEncoding = null)
        {
            var encoding = defaultEncoding ?? Encoding.Default;
            try
            {
                var cdet = new CharsetDetector();
                cdet.Feed(stream);
                cdet.DataEnd();
                if (cdet.Charset != null)
                {
                    encoding = Encoding.GetEncoding(cdet.Charset);
                }
            }
            catch (Exception)
            {
                // Do nothing
            }
            finally
            {
                stream.Position = 0;
            }
            return encoding;
        }
    }
}