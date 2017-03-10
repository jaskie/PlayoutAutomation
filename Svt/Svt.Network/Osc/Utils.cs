using System;

namespace Svt.Network.Osc
{
    public class Utils
    {
        public static DateTime TimetagToDateTime(UInt64 val)
        {
            if (val == 1)
                return DateTime.Now;

            UInt32 seconds = (UInt32)(val >> 32);
            var time = DateTime.Parse("1900-01-01 00:00:00");
            time = time.AddSeconds(seconds);
            var fraction = TimetagToFraction(val);
            time = time.AddSeconds(fraction);
            return time;
        }

        public static double TimetagToFraction(UInt64 val)
        {
            if (val == 1)
                return 0.0;

            UInt32 seconds = (UInt32)(val & 0x00000000FFFFFFFF);
            double fraction = (double)seconds / (UInt32)(0xFFFFFFFF);
            return fraction;
        }

        public static UInt64 DateTimeToTimetag(DateTime value)
        {
            UInt64 seconds = (UInt32)(value - DateTime.Parse("1900-01-01 00:00:00.000")).TotalSeconds;
            UInt64 fraction = (UInt32)(0xFFFFFFFF * ((double)value.Millisecond / 1000));

            UInt64 output = (seconds << 32) + fraction;
            return output;
        }

        public static int AlignedStringLength(string val)
        {
            int len = val.Length + (4 - val.Length % 4);
            if (len <= val.Length) len += 4;

            return len;
        }
    }
}
