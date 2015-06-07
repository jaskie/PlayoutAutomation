using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TAS.Server
{
    public static class FileUtils
    {
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
        public static bool DateTimeEqualToSeconds(this DateTime self, DateTime dt)
        {
            return self.Date.Equals(dt.Date) && self.Hour == dt.Hour && self.Minute == dt.Minute && self.Second == dt.Second;
        }

        public static DateTime FromFileTime(DateTime dt, DateTimeKind kind)
        {
            return DateTime.SpecifyKind(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second), kind);
        }
    }
}
