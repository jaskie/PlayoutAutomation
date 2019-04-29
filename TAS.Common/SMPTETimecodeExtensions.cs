using System;
using System.Text.RegularExpressions;

namespace TAS.Common
{
    public static class SMPTETimecodeExtensions
    {
        const string LTCREGEXSTRING = "\\d{8}";

        static readonly Regex validateLTC = new Regex(LTCREGEXSTRING, RegexOptions.ECMAScript);

        public static string ToSmpteTimecodeString(this TimeSpan t, TVideoFormat format)
        {
            if (!Enum.IsDefined(typeof(TVideoFormat), format))
                format = TVideoFormat.PAL_FHA;
            return t.ToSmpteTimecodeString(VideoFormatDescription.Descriptions[format].FrameRate);
        }

        public static string ToSmpteTimecodeString(this TimeSpan t, RationalNumber rate)
        {
            if (rate.IsZero)
                return t.ToString();
            var minus = t < TimeSpan.Zero;
            var value = minus ? -t : t;
            var days = value.Days;
            var hours = value.Hours;
            var minutes = value.Minutes;
            var seconds = value.Seconds;
            var frames = ((value.Ticks % TimeSpan.TicksPerSecond) * rate.Num + (rate.Den * TimeSpan.TicksPerSecond / 2)) / (rate.Den * TimeSpan.TicksPerSecond); // rounding
            if (days > 0)
                return $"{(minus ? "-" : "")}{days}:{hours:D2}:{minutes:D2}:{seconds:D2}:{frames:D2}";
            else
                return $"{(minus ? "-" : "")}{hours:D2}:{minutes:D2}:{seconds:D2}:{frames:D2}";
        }

        public static long ToSmpteFrames(this TimeSpan t, TVideoFormat format)
        {
            if (!Enum.IsDefined(typeof(TVideoFormat), format))
                format = TVideoFormat.PAL_FHA;
            return t.ToSmpteFrames(VideoFormatDescription.Descriptions[format].FrameRate);
        }

        public static long ToSmpteFrames(this TimeSpan t, RationalNumber rate)
        {
            if (rate.IsInvalid)
                return 0L;
            return (t.Ticks +1) * rate.Num / (TimeSpan.TicksPerSecond * rate.Den);
        }

        public static TimeSpan SmpteFramesToTimeSpan(this long totalFrames, TVideoFormat format)
        {
            if (!Enum.IsDefined(typeof(TVideoFormat), format))
                format = TVideoFormat.PAL_FHA;
            return totalFrames.SmpteFramesToTimeSpan(VideoFormatDescription.Descriptions[format].FrameRate);
        }

        public static TimeSpan SmpteFramesToTimeSpan(this long totalFrames, RationalNumber rate)
        {
            if (rate.IsZero)
                return TimeSpan.Zero;
            return TimeSpan.FromTicks(totalFrames * TimeSpan.TicksPerSecond * rate.Den / rate.Num);
        }

        public static TimeSpan Round (this TimeSpan t, RationalNumber frameRate)
        {
            if (frameRate.IsInvalid || frameRate.IsZero)
                return TimeSpan.Zero;
            return TimeSpan.FromTicks(((t.Ticks +1) * frameRate.Num / (TimeSpan.TicksPerSecond * frameRate.Den)) * TimeSpan.TicksPerSecond * frameRate.Den / frameRate.Num);
        }

        public static TimeSpan SmpteFramesToTimeSpan(this long totalFrames, string frameRate)
        {
            long rate = 25;
            if (frameRate.Length <= 0)
                return TimeSpan.FromTicks(totalFrames * TimeSpan.TicksPerSecond / rate);
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
            return TimeSpan.FromTicks(totalFrames * TimeSpan.TicksPerSecond / rate);
        }

        public static bool IsValidSmpteTimecode(this string timeCode, TVideoFormat format)
        {
            if (!Enum.IsDefined(typeof(TVideoFormat), format))
                format = TVideoFormat.PAL_FHA;
            return IsValidSmpteTimecode(timeCode, VideoFormatDescription.Descriptions[format].FrameRate);
        }

        public static bool IsValidSmpteTimecode(this string timeCode, RationalNumber rate)
        {
            if (rate.IsZero)
                return TimeSpan.TryParse(timeCode, out var _);
            var times = timeCode.Split(':');
            if (times.Length < 4 || times.Length > 5)
                return false;

            var index = -1;
            var days = 0;
            if (!((times.Length == 5 && int.TryParse(times[++index], out days) || times.Length == 4)
                  && int.TryParse(times[++index], out var hours)
                  && int.TryParse(times[++index], out var minutes)
                  && int.TryParse(times[++index], out var seconds)
                  && int.TryParse(times[++index], out var frames)))
                return false;

            return (days == 0 || hours >= 0) 
                && (Math.Abs(hours) < 24) 
                && minutes < 60 && minutes >= 0 
                && seconds < 60 && seconds >= 0 
                && frames < (int)(rate.Num / rate.Den) && frames >= 0;
        }

        public static TimeSpan SmpteTimecodeToTimeSpan(this string timeCode, TVideoFormat format)
        {
            if (!Enum.IsDefined(typeof(TVideoFormat), format))
                format = TVideoFormat.PAL_FHA;
            return SmpteTimecodeToTimeSpan(timeCode, VideoFormatDescription.Descriptions[format].FrameRate);
        }

        public static TimeSpan SmpteTimecodeToTimeSpan(this string timeCode, RationalNumber rate)
        {
            if (rate.IsZero)
            {
                return TimeSpan.Parse(timeCode);
            }
            else
            {
                var times = timeCode.Split(':');
                if (times.Length < 4 || times.Length > 5)
                    throw new FormatException("Bad SMPTE timecode format");

                var index = -1;
                var days = 0;
                if (!((times.Length == 5 && int.TryParse(times[++index], out days) || times.Length == 4)
                    && int.TryParse(times[++index], out var hours)
                    && int.TryParse(times[++index], out var minutes)
                    && int.TryParse(times[++index], out var seconds)
                    && int.TryParse(times[++index], out var frames)))
                    throw new FormatException("Bad SMPTE timecode content");

                if ((days != 0 && hours < 0)
                    || (Math.Abs(hours) >= 24)
                    || minutes >= 60 || minutes < 0 || seconds >= 60 || seconds < 0 || frames >= (int)(rate.Num / rate.Den) || frames < 0)
                    throw new FormatException("SMPTE Timecode out of range");

                if (days < 0)
                    return -new TimeSpan(Math.Abs(days), hours, minutes, seconds, frames * (int)rate.Den * 1000 / (int)rate.Num);
                if (hours < 0) // was raised exception if days <> 0 and hours < 0
                    return -new TimeSpan(0, Math.Abs(hours), minutes, seconds, frames * (int)rate.Den * 1000 / (int)rate.Num);
                return new TimeSpan(days, hours, minutes, seconds, frames * (int)rate.Den * 1000 / (int)rate.Num);
            }
        }

        public static TimeSpan LtcTimecodeToTimeSpan(this string timeCode, RationalNumber rate)
        {
            if (!validateLTC.IsMatch(timeCode))
                throw new FormatException("Bad LTC timecode format");

            var frames = int.Parse(timeCode.Substring(0, 2));
            var seconds = int.Parse(timeCode.Substring(2, 2));
            var minutes = int.Parse(timeCode.Substring(4, 2));
            var hours = int.Parse(timeCode.Substring(6));
            var days = hours / 24;
            hours = hours % 24;

            if ((hours > 24) || (minutes >= 60) || (seconds >= 60) || (frames >= (int)(rate.Num / rate.Den)))
                throw new FormatException("LTC Timecode out of range");

            return new TimeSpan(days, hours, minutes, seconds, frames * (int)rate.Den * 1000 / (int)rate.Num);
        }
    }

}
