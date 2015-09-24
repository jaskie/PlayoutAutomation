using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TAS.Common
{
    public static class SMPTETimecodeExtensions
    {
        const long SecondsPerDay = 86400L;
        const long SecondsPerHour = 3600L;
        const long SecondsPerMinute = 60L;

        const string LTCREGEXSTRING = "\\d{8}";

        static readonly Regex validateLTC = new Regex(LTCREGEXSTRING, RegexOptions.ECMAScript);

        public static string ToSMPTETimecodeString(this TimeSpan t, TSMPTEFrameRate rate = TSMPTEFrameRate.SMPTERate25fps)
        {
            int framesPerSecond = (int) rate;
            bool minus = t < TimeSpan.Zero;
            TimeSpan value = minus ? -t : t;
            int days = value.Days;
            int hours = value.Hours;
            int minutes = value.Minutes;
            int seconds = value.Seconds;
            int frames = value.Milliseconds * framesPerSecond / 1000;
            if (days > 0)
                return string.Format("{0}{1}:{2:D2}:{3:D2}:{4:D2}:{5:D2}", minus ? "-" : "", days, hours, minutes, seconds, frames);
            else
                return string.Format("{0}{1:D2}:{2:D2}:{3:D2}:{4:D2}", minus ? "-" : "", hours, minutes, seconds, frames);
        }

        public static long ToSMPTEFrames(this TimeSpan t, TSMPTEFrameRate rate = TSMPTEFrameRate.SMPTERate25fps)
        {
            return t.Ticks * (long)rate / TimeSpan.TicksPerSecond;
        }

        public static TimeSpan SMPTEFramesToTimeSpan(this long totalFrames, TSMPTEFrameRate rate = TSMPTEFrameRate.SMPTERate25fps)
        {
            return TimeSpan.FromTicks(totalFrames * TimeSpan.TicksPerSecond / (long)rate);
        }

        public static TimeSpan SMPTEFramesToTimeSpan(this long totalFrames, string frameRate)
        {
            long rate = 25;
            if (frameRate.Length > 0)
            {
                switch (frameRate[frameRate.Length - 1])
                {
                    case 'i':
                    case 'I':
                        if (long.TryParse(frameRate.Substring(0, frameRate.Length - 1), out rate))
                            rate = rate / 2;
                        break;
                    case 'p':
                    case 'P':
                        long.TryParse(frameRate.Substring(0, frameRate.Length - 1), out rate);
                        break;
                    default:
                        long.TryParse(frameRate, out rate);
                        break;
                }
            }
            return TimeSpan.FromTicks(totalFrames * TimeSpan.TicksPerSecond / rate);
        }

        public static bool IsValidSMPTETimecode(this string timeCode, TSMPTEFrameRate rate = TSMPTEFrameRate.SMPTERate25fps)
        {
            string[] times = timeCode.Split(':');
            if (times.Length < 4 || times.Length > 5)
                return false;
            
            int index = -1;
            int days = 0;
            int hours, minutes, seconds, frames;
            if (!((times.Length == 5 && int.TryParse(times[++index], out days) || times.Length == 4)
                && int.TryParse(times[++index], out hours)
                && int.TryParse(times[++index], out minutes)
                && int.TryParse(times[++index], out seconds)
                && int.TryParse(times[++index], out frames)))
                return false;

            if ((days != 0 && hours < 0)
                || (Math.Abs(hours) >= 24) 
                || minutes >= 60  || minutes < 0 || seconds >= 60 || seconds < 0 || frames >= (int)rate || frames < 0)
                return false;

            return true;
        }

        public static TimeSpan SMPTETimecodeToTimeSpan(this string timeCode, TSMPTEFrameRate rate = TSMPTEFrameRate.SMPTERate25fps)
        {
         
            string[] times = timeCode.Split(':');
            if (times.Length < 4 || times.Length > 5)
                throw new FormatException("Bad SMPTE timecode format");

            int index = -1;
            int days = 0;
            int hours, minutes, seconds, frames;
            if (!((times.Length == 5 && int.TryParse(times[++index], out days) || times.Length == 4)
                && int.TryParse(times[++index], out hours)
                && int.TryParse(times[++index], out minutes)
                && int.TryParse(times[++index], out seconds)
                && int.TryParse(times[++index], out frames)))
                throw new FormatException("Bad SMPTE timecode content");

            if ((days != 0 && hours < 0)
                || (Math.Abs(hours) >= 24)
                || minutes >= 60 || minutes < 0 || seconds >= 60 || seconds < 0 || frames >= (int)rate || frames < 0)
                throw new FormatException("SMPTE Timecode out of range");
            
            if (days < 0)
                return - new TimeSpan(Math.Abs(days), hours, minutes, seconds, frames * 1000 / (int)rate);
            if (hours < 0) // was raised exception if days <> 0 and hours < 0
                return - new TimeSpan(0, Math.Abs(hours), minutes, seconds, frames * 1000 / (int)rate);
            return new TimeSpan(days, hours, minutes, seconds, frames * 1000 / (int)rate);
        }

        public static TimeSpan LTCTimecodeToTimeSpan(this string timeCode, TSMPTEFrameRate rate = TSMPTEFrameRate.SMPTERate25fps)
        {
            if (!validateLTC.IsMatch(timeCode))
                throw new FormatException("Bad LTC timecode format");

            int frames = Int32.Parse(timeCode.Substring(0, 2));
            int seconds = Int32.Parse(timeCode.Substring(2, 2));
            int minutes = Int32.Parse(timeCode.Substring(4, 2));
            int hours = Int32.Parse(timeCode.Substring(6));
            int days = hours / 24;
            hours = hours % 24;

            if ((hours > 24) || (minutes >= 60) || (seconds >= 60) || (frames >= (int)rate))
                throw new FormatException("LTC Timecode out of range");
            
            return new TimeSpan(days, hours, minutes, seconds, frames * 1000 / (int)rate);
        }
    }

}
