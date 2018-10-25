using System;

namespace TAS.Common
{
    public static class DateTimeExtensions
    {
        public static bool DateTimeEqualToDays(this DateTime self, DateTime dt)
        {
            return (self.Date - dt).Days == 0;
        }

        public static DateTime FromFileTime(this DateTime dt, DateTimeKind kind)
        {
            return DateTime.SpecifyKind(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second), kind);
        }
    }
}