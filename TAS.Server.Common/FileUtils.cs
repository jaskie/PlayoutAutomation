using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TAS.Common;

namespace TAS.Server.Common
{
    public static class FileUtils
    {
        public readonly static string[] VideoFileTypes = { ".mov", ".mxf", ".mkv", ".mp4", ".wmv", ".avi", ".lxf" };
        public readonly static string[] StillFileTypes = { ".tif", ".tga", ".png", ".tiff", ".jpg", ".gif", ".bmp" };

        public static string DefaultFileExtension(TMediaType type)
        {
            if (type == TMediaType.Movie || type == TMediaType.Unknown)
                return VideoFileTypes[0];
            if (type == TMediaType.Still)
                return StillFileTypes[0];
            throw new NotImplementedException(string.Format("MediaDirectory:DefaultFileExtension {0}", type));
        }

        public static string SanitizeFileName(string text)
        {
            char[] arr = text.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
                if (Path.GetInvalidFileNameChars().Contains(arr[i]))
                    arr[i] = '_';
            return new string(arr);
        }
        
    }

    public static class DateTimeExtensions
    {
        public static bool DateTimeEqualToDays(this DateTime self, DateTime dt)
        {
            return (self.Date - dt).Days == 0;
        }

        public static DateTime FromFileTime(DateTime dt, DateTimeKind kind)
        {
            return DateTime.SpecifyKind(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second), kind);
        }
    }

    

}
