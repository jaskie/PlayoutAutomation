﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TAS.Common
{
    public static class FileUtils
    {
        public static readonly string[] VideoFileTypes = { ".mov", ".mxf", ".mkv", ".mp4", ".wmv", ".avi", ".lxf", ".mpg", ".mpeg" };
        public static readonly string[] StillFileTypes = { ".png", ".tif", ".tga", ".tiff", ".jpg", ".gif", ".bmp" };
        public static readonly string[] AudioFileTypes = { ".mp3" };
        public static readonly string[] AnimationFileTypes = { ".ft", ".htm", ".html" };
        public static readonly string RundownFileExtension = ".rundown";
        public static readonly string TempFileExtension = ".tmp";
        public static readonly string ConfigurationPath = Path.Combine(Directory.GetCurrentDirectory(), "Configuration");
        public static readonly string ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TVPlay");
        
        public static string DefaultFileExtension(TMediaType type)
        {
            switch (type)
            {
                case TMediaType.Movie:
                case TMediaType.Unknown:
                    return VideoFileTypes[0];
                case TMediaType.Still:
                    return StillFileTypes[0];
                case TMediaType.Audio:
                    return AudioFileTypes[0];
                case TMediaType.Animation:
                    return AnimationFileTypes[0];
                default:
                    throw new NotImplementedException($"FileUtils::DefaultFileExtension {type}");
            }
        }

        public static string SanitizeFileName(string text)
        {
            char[] arr = text.Trim().ToCharArray();
            for (int i = 0; i < arr.Length; i++)
                if (Path.GetInvalidFileNameChars().Contains(arr[i]))
                    arr[i] = '_';
            return new string(arr).Trim();
        }

        public static string GetUniqueFileName(string folder, string fileName, int maxAttempts = 1024)
        {
            // get filename base and extension
            var fileBase = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            // build hash set of filenames for performance
            if (!Directory.Exists(folder))
                return fileName;
            var files = new HashSet<string>(Directory.GetFiles(folder));

            for (var index = 0; index < maxAttempts; index++)
            {
                // first try with the original filename, else try incrementally adding an index
                var name = (index == 0)
                    ? fileName
                    : $"{fileBase}_{index}{ext}";

                // check if exists
                var fullPath = Path.Combine(folder, name);
                if (files.Contains(fullPath))
                    continue;
                return name;
            }
            throw new ApplicationException("Could not create unique filename in " + maxAttempts + " attempts");
        }

        public static string GetFileNameWithoutExtension(string fileName, TMediaType mediaType)
        {
            if (fileName == null)
                return string.Empty;
            var fileExt = Path.GetExtension(fileName).ToLowerInvariant();
            switch (mediaType)
            {
                case TMediaType.Movie:
                case TMediaType.Unknown:
                    return VideoFileTypes.Contains(fileExt) ? Path.GetFileNameWithoutExtension(fileName) : Path.GetFileName(fileName);
                case TMediaType.Still:
                    return StillFileTypes.Contains(fileExt) ? Path.GetFileNameWithoutExtension(fileName) : Path.GetFileName(fileName);
                case TMediaType.Audio:
                    return AudioFileTypes.Contains(fileExt) ? Path.GetFileNameWithoutExtension(fileName) : Path.GetFileName(fileName);
                case TMediaType.Animation:
                    return AnimationFileTypes.Contains(fileExt) ? Path.GetFileNameWithoutExtension(fileName) : Path.GetFileName(fileName);
                default:
                    throw new NotImplementedException($"FileUtils::ExtractFilenameWithoutExtension {mediaType}");
            }
        }

        public static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static TMediaType GetMediaType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLower();
            if (ext == null)
                return TMediaType.Unknown;
            return VideoFileTypes.Contains(ext)
                ? TMediaType.Movie
                : StillFileTypes.Contains(ext)
                    ? TMediaType.Still
                    : AudioFileTypes.Contains(ext)
                        ? TMediaType.Audio
                        : AnimationFileTypes.Contains(ext)
                            ? TMediaType.Animation
                            : TMediaType.Unknown;
        }


        [DllImport("shlwapi.dll", EntryPoint = "PathRelativePathTo")]
        private static extern bool PathRelativePathTo(StringBuilder lpszDst,
            string from, uint attrFrom,
            string to, uint attrTo);

        public static string GetRelativePath(string from, string to)
        {
            if (to.StartsWith(from))
                return to.Substring(from.Length + 1);
            var builder = new StringBuilder(1024);
            return PathRelativePathTo(builder, from, 0, to, 0) ? builder.ToString(): throw new ApplicationException();
        }
    }
}
