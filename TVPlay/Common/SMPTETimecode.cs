using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Globalization;


namespace TAS.Common
{
    public enum SMPTEFrameRate {SMPTERate25fps, SMPTERate24fps, SMPTERate30fps, Unknown};

    public class SMPTETimecode : IComparable
    {
        private const string SMPTEREGEXSTRING = "(?<Hours>\\d{1,2}):(?<Minutes>\\d{1,2}):(?<Seconds>\\d{1,2})(?::|;)(?<Frames>\\d{1,2})";
        private const string LTCREGEXSTRING = "\\d{8}";
        // Epsilon value to deal with rounding precision issues with decimal and double values.
        //private const decimal EPSILON = 0.00000000000000000000001M;

        // Regular expression object used for validating timecode.
        private static readonly Regex validateTimecode = new Regex(SMPTEREGEXSTRING, RegexOptions.ECMAScript);
        private static readonly Regex validateLTC = new Regex(LTCREGEXSTRING, RegexOptions.ECMAScript);

        // The private Timespan used to track absolute time for this instance.
        private readonly long absoluteTicks;

        // The frame rate for this instance.
        private SMPTEFrameRate frameRate;

        public SMPTETimecode(int hours, int minutes, int seconds, int frames, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            string timeCode = String.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", hours, minutes, seconds, frames);
            this.frameRate = rate;
            this.absoluteTicks = Smpte12mToTicks(timeCode, this.frameRate);
        }

        public SMPTETimecode(int days, int hours, int minutes, int seconds, int frames, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            string timeCode = String.Format("{0}:{1:D2}:{2:D2}:{3:D2}:{4:D2}", days, hours, minutes, seconds, frames);
            this.frameRate = rate;
            this.absoluteTicks = Smpte12mToTicks(timeCode, this.frameRate);
        }

        public SMPTETimecode(int windowsMediaTimeCode, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            byte[] timeCodeBytes = BitConverter.GetBytes(windowsMediaTimeCode);
            string timeCode = String.Format("{3:x2}:{2:x2}:{1:x2}:{0:x2}", timeCodeBytes[0], timeCodeBytes[1], timeCodeBytes[2], timeCodeBytes[3]);
            this.frameRate = rate;
            this.absoluteTicks = Smpte12mToTicks(timeCode, this.frameRate);
        }

        public SMPTETimecode(TimeSpan timeSpan, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            this.frameRate = rate;
            this.absoluteTicks = SMPTETimecode.FromTimeSpan(timeSpan, rate).absoluteTicks;
        }

        public SMPTETimecode(string timeCodeAndRate)
        {
            string[] timeAndRate = timeCodeAndRate.Split('@');

            string time = string.Empty;
            string rate = string.Empty;

            if (timeAndRate.Length == 1)
            {
                time = timeAndRate[0];
                rate = "25";
            }
            else if (timeAndRate.Length == 2)
            {
                time = timeAndRate[0];
                rate = timeAndRate[1];
            }

            else if (rate == "25")
            {
                this.frameRate = SMPTEFrameRate.SMPTERate25fps;
            }
            else if (rate == "24")
            {
                this.frameRate = SMPTEFrameRate.SMPTERate24fps;
            }
            else if (rate == "30")
            {
                this.frameRate = SMPTEFrameRate.SMPTERate30fps;
            }

            this.absoluteTicks = Smpte12mToTicks(time, this.frameRate);
        }

        public SMPTETimecode(string timeCode, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            this.frameRate = rate;
            this.absoluteTicks = Smpte12mToTicks(timeCode, this.frameRate);
        }
        
        public SMPTETimecode(long ticks, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            this.absoluteTicks = ticks;
            this.frameRate = rate;
        }

        public static double MinValue
        {
            get { return 0; }
        }

        public double Duration
        {
            get { return Convert.ToDouble(this.absoluteTicks); }
        }

        public SMPTEFrameRate FrameRate
        {
            get { return this.frameRate; }
            set { this.frameRate = value; }
        }

        public int DaysSegment
        {
            get
            {
                string timeCode = TicksToSMPTE(this.absoluteTicks, this.frameRate);

                string days = "0";

                if (timeCode.Length > 11)
                {
                    int index = timeCode.IndexOf(":");
                    days = timeCode.Substring(0, index);
                }

                return Int32.Parse(days);
            }
        }

        public int HoursSegment
        {
            get
            {
                string timeCode = TicksToSMPTE(this.absoluteTicks, this.frameRate);

                if (timeCode.Length > 11)
                {
                    int index = timeCode.IndexOf(":") + 1;
                    timeCode = timeCode.Substring(index, timeCode.Length - index);
                }

                string hours = timeCode.Substring(0, 2);

                return Int32.Parse(hours);
            }
        }

        public int MinutesSegment
        {
            get
            {
                string timeCode = TicksToSMPTE(this.absoluteTicks, this.frameRate);

                if (timeCode.Length > 11)
                {
                    int index = timeCode.IndexOf(":") + 1;
                    timeCode = timeCode.Substring(index, timeCode.Length - index);
                }

                string minutes = timeCode.Substring(3, 2);

                return Int32.Parse(minutes);
            }
        }

        public int SecondsSegment
        {
            get
            {
                string timeCode = TicksToSMPTE(this.absoluteTicks, this.frameRate);

                if (timeCode.Length > 11)
                {
                    int index = timeCode.IndexOf(":") + 1;
                    timeCode = timeCode.Substring(index, timeCode.Length - index);
                }

                string seconds = timeCode.Substring(6, 2);

                return Int32.Parse(seconds);
            }
        }

        public int FramesSegment
        {
            get
            {
                string timeCode = TicksToSMPTE(this.absoluteTicks, this.frameRate);

                if (timeCode.Length > 11)
                {
                    int index = timeCode.IndexOf(":") + 1;
                    timeCode = timeCode.Substring(index, timeCode.Length - index);
                }

                string frames = timeCode.Substring(9, 2);

                return Int32.Parse(frames);
            }
        }

        public double TotalDays
        {
            get
            {
                long framecount = TicksToFrames(this.absoluteTicks, this.frameRate);
                return (framecount / 108000D) / 24;
            }
        }

        public double TotalHours
        {
            get
            {
                long framecount = TicksToFrames(this.absoluteTicks, this.frameRate);

                double hours;

                switch (this.frameRate)
                {
                    case SMPTEFrameRate.SMPTERate24fps:
                        hours = framecount / 86400D;
                        break;
                    case SMPTEFrameRate.SMPTERate25fps:
                        hours = framecount / 90000D;
                        break;
                    case SMPTEFrameRate.SMPTERate30fps:
                        hours = framecount / 108000D;
                        break;
                    default:
                        hours = framecount / 90000D;
                        break;
                }

                return hours;
            }
        }

        public double TotalMinutes
        {
            get
            {
                long framecount = TicksToFrames(this.absoluteTicks, this.frameRate);

                double minutes;

                switch (this.frameRate)
                {
                    case SMPTEFrameRate.SMPTERate24fps:
                        minutes = framecount / 1400D;
                        break;
                    case SMPTEFrameRate.SMPTERate25fps:
                        minutes = framecount / 1500D;
                        break;
                    case SMPTEFrameRate.SMPTERate30fps:
                        minutes = framecount / 1800D;
                        break;
                    default:
                        minutes = framecount / 1500D;
                        break;
                }

                return minutes;
            }
        }

        public double TotalSeconds
        {
            get
            {
                return Convert.ToDouble(decimal.Round(this.absoluteTicks/1e7m, 6));
            }
        }

      
        public long TotalFrames
        {
            get
            {
                return TicksToFrames(this.absoluteTicks, this.frameRate);
            }
        }

        public static decimal MaxValue(SMPTEFrameRate frameRate = SMPTEFrameRate.SMPTERate25fps)
        {
            switch (frameRate)
            {
                case SMPTEFrameRate.SMPTERate24fps:
                    return 86399.958333333300000M;

                case SMPTEFrameRate.SMPTERate25fps:
                    return 86399.960000000000000M;


                case SMPTEFrameRate.SMPTERate30fps:
                    return 86399.966666666700000M;

                default:
                    return 86399;
            }
        }

        public static SMPTETimecode operator -(SMPTETimecode t1, SMPTETimecode t2)
        {
            var t3 = new SMPTETimecode(t1.absoluteTicks - t2.absoluteTicks, t1.FrameRate);

            if (t3.TotalSeconds < MinValue)
            {
                throw new OverflowException("Timecode cannot be negative.");
            }

            return t3;
        }

        public static bool operator !=(SMPTETimecode t1, SMPTETimecode t2)
        {
            var timeCode1 = new SMPTETimecode(t1.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);
            var timeCode2 = new SMPTETimecode(t2.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);

            if (timeCode1.TotalFrames != timeCode2.TotalFrames)
            {
                return true;
            }

            return false;
        }

        public static SMPTETimecode operator +(SMPTETimecode t1, SMPTETimecode t2)
        {
            var t3 = new SMPTETimecode(t1.absoluteTicks + t2.absoluteTicks, t1.FrameRate);

            return t3;
        }

        public static bool operator <(SMPTETimecode t1, SMPTETimecode t2)
        {
            var timeCode1 = new SMPTETimecode(t1.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);
            var timeCode2 = new SMPTETimecode(t2.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);

            if (timeCode1.TotalFrames < timeCode2.TotalFrames)
            {
                return true;
            }
            return false;
        }

        public static bool operator <=(SMPTETimecode t1, SMPTETimecode t2)
        {
            var timeCode1 = new SMPTETimecode(t1.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);
            var timeCode2 = new SMPTETimecode(t2.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);

            if (timeCode1.TotalFrames < timeCode2.TotalFrames || (timeCode1.TotalFrames == timeCode2.TotalFrames))
            {
                return true;
            }

            return false;
        }

        public static bool operator ==(SMPTETimecode t1, SMPTETimecode t2)
        {
            var timeCode1 = new SMPTETimecode(t1.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);
            var timeCode2 = new SMPTETimecode(t2.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);

            if (timeCode1.TotalFrames == timeCode2.TotalFrames)
            {
                return true;
            }

            return false;
        }

        public static bool operator >(SMPTETimecode t1, SMPTETimecode t2)
        {
            var timeCode1 = new SMPTETimecode(t1.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);
            var timeCode2 = new SMPTETimecode(t2.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);

            if (timeCode1.TotalFrames > timeCode2.TotalFrames)
            {
                return true;
            }
            return false;
        }

        public static bool operator >=(SMPTETimecode t1, SMPTETimecode t2)
        {
            var timeCode1 = new SMPTETimecode(t1.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);
            var timeCode2 = new SMPTETimecode(t2.absoluteTicks, SMPTEFrameRate.SMPTERate25fps);

            if (timeCode1.TotalFrames > timeCode2.TotalFrames || (timeCode1.TotalFrames == timeCode2.TotalFrames))
            {
                return true;
            }

            return false;
        }

        public static int Compare(SMPTETimecode t1, SMPTETimecode t2)
        {
            if (t1 < t2)
            {
                return -1;
            }

            if (t1 == t2)
            {
                return 0;
            }

            return 1;
        }

        public static bool Equals(SMPTETimecode t1, SMPTETimecode t2)
        {
            if (t1 == t2)
            {
                return true;
            }

            return false;
        }

        public static SMPTETimecode FromFrames(long value, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            long ticks = FramesToTicks(value, rate);
            return new SMPTETimecode(ticks, rate);
        }

        public static SMPTETimecode FromTicks(long value, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            return new SMPTETimecode(value, rate);
        }

        public static SMPTETimecode FromTimeSpan(TimeSpan value, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            return new SMPTETimecode(value.Ticks, rate);
        }

        public static bool ValidateSMPTETimecode(string timeCode, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            if (!validateTimecode.IsMatch(timeCode))
            {
                return false;
            }

            string[] times = timeCode.Split(':', ';');

            int index = -1;
            int days = 0;

            if (times.Length > 4)
            {
                days = Int32.Parse(times[++index]);
            }
            int hours;
            int minutes;
            int seconds;
            int frames;
            if (!
                (Int32.TryParse(times[++index], out hours)
                && Int32.TryParse(times[++index], out minutes)
                && Int32.TryParse(times[++index], out seconds)
                && Int32.TryParse(times[++index], out frames)
                ))
                return false;

            if ((days < 0) || (hours >= 24) || (minutes >= 60) || (seconds >= 60) || (frames >= ((rate == SMPTEFrameRate.SMPTERate25fps) ? 25 : ((rate == SMPTEFrameRate.SMPTERate24fps) ? 24 : 30))))
                return false;

            return true;
        }

        public static SMPTEFrameRate ParseFramerate(double rate)
        {
            int rateRounded = (int)Math.Floor(rate);

            switch (rateRounded)
            {
                case 24: return SMPTEFrameRate.SMPTERate24fps;
                case 25: return SMPTEFrameRate.SMPTERate25fps;
                case 30: return SMPTEFrameRate.SMPTERate30fps;
            }

            return SMPTEFrameRate.Unknown;
        }

        public SMPTETimecode Add(SMPTETimecode ts)
        {
            return this + ts;
        }

        public int CompareTo(object value)
        {
            if (!(value is SMPTETimecode))
            {
                throw new ArgumentException("Can't compare with non-SMPTETimecode value");
            }

            SMPTETimecode t1 = (SMPTETimecode)value;

            if (this < t1)
            {
                return -1;
            }

            if (this == t1)
            {
                return 0;
            }

            return 1;
        }

        public int CompareTo(SMPTETimecode value)
        {
            if (this < value)
            {
                return -1;
            }

            if (this == value)
            {
                return 0;
            }

            return 1;
        }

        public override bool Equals(object value)
        {
            if (!(value is SMPTETimecode))
            {
                throw new ArgumentException("Can't determine equation with non-SMPTETimecode value");
            }

            if (this == (SMPTETimecode)value)
            {
                return true;
            }

            return false;
        }

        public bool Equals(SMPTETimecode obj)
        {
            if (this == obj)
            {
                return true;
            }

            return false;
        }

        public SMPTETimecode Subtract(SMPTETimecode ts)
        {
            return this - ts;
        }

        public override string ToString()
        {
            return TicksToSMPTE(this.absoluteTicks, this.frameRate);
        }

        public string ToString(SMPTEFrameRate rate)
        {
            return TicksToSMPTE(this.absoluteTicks, rate);
        }

        public TimeSpan ToTimeSpan()
        {
            return TimeSpan.FromTicks(this.absoluteTicks);
        }

        public static TimeSpan TimecodeToTimeSpan(string timeCode, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            if (ValidateSMPTETimecode(timeCode, rate))
                return TimeSpan.FromTicks(Smpte12mToTicks(timeCode, rate));
            else
                return TimeSpan.Zero;
        }

        public static string TimeSpanToTimeCode(TimeSpan timeSpan, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            return TicksToSMPTE(timeSpan.Ticks, rate);
        }

        public static TimeSpan LTCToTimeSpan(string LTC, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            return TimeSpan.FromTicks(LTCToTicks(LTC, rate));
        }

        public static string FramesToTimeCode(long frames, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            long ticks;

            if (rate == SMPTEFrameRate.SMPTERate24fps)
            {
                ticks = frames * 10000000 / 24;
            }
            else if (rate == SMPTEFrameRate.SMPTERate25fps)
            {
                ticks = frames * 10000000 / 25;
            }
            else if (rate == SMPTEFrameRate.SMPTERate30fps)
            {
                ticks = frames * 10000000 / 30;
            }
            else
            {
                ticks = frames * 10000000 / 25;
            }
            return TicksToSMPTE(ticks, rate);
        }

        private static long Smpte12mToTicks(string timeCode, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            switch (rate)
            {
                case SMPTEFrameRate.SMPTERate24fps:
                    return Smpte12M_24_ToTicks(timeCode);
                case SMPTEFrameRate.SMPTERate30fps:
                    return Smpte12M_30_ToTicks(timeCode);
                case SMPTEFrameRate.SMPTERate25fps:
                default:
                    return Smpte12M_25_ToTicks(timeCode);
            }
        }
        private static long LTCToTicks(string timeCode, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            switch (rate)
            {
                case SMPTEFrameRate.SMPTERate24fps:
                    return LTC_24_ToTicks(timeCode);
                case SMPTEFrameRate.SMPTERate30fps:
                    return LTC_30_ToTicks(timeCode);
                case SMPTEFrameRate.SMPTERate25fps:
                default:
                    return LTC_25_ToTicks(timeCode);
            }
        }
        
        private static void ParseLTCString(string lTC, out int hours, out int minutes, out int seconds, out int frames)
        {
            if (!validateLTC.IsMatch(lTC))
            {
                throw new FormatException("Bad LTC timecode format");
            }
            
            frames = Int32.Parse(lTC.Substring(0, 2));
            seconds = Int32.Parse(lTC.Substring(2, 2));
            minutes = Int32.Parse(lTC.Substring(4, 2));
            hours = Int32.Parse(lTC.Substring(6, 2));

            if ((hours > 99) || (minutes >= 60) || (seconds >= 60) || (frames >= 30))
            {
                throw new FormatException("LTC Timecode out of range");
            }
        }

        private static void ParseTimecodeString(string timeCode, out int days, out int hours, out int minutes, out int seconds, out int frames)
        {
            if (!validateTimecode.IsMatch(timeCode))
            {
                throw new FormatException("Bad SMPTE timecode format");
            }

            string[] times = timeCode.Split(':', ';');

            int index = -1;

            days = 0;

            if (times.Length > 4)
            {
                days = Int32.Parse(times[++index]);
            }

            hours = Int32.Parse(times[++index]);
            minutes = Int32.Parse(times[++index]);
            seconds = Int32.Parse(times[++index]);
            frames = Int32.Parse(times[++index]);

            if ((days < 0) || (hours >= 24) || (minutes >= 60) || (seconds >= 60) || (frames >= 30))
            {
                throw new FormatException("SMPTE Timecode out of range");
            }
        }

        private static void ParseTimecodeString(string timeCode, out int hours, out int minutes, out int seconds, out int frames)
        {
            if (!validateTimecode.IsMatch(timeCode))
            {
                throw new FormatException("Bad SMPTE timecode format");
            }

            string[] times = timeCode.Split(':', ';');

            hours = Int32.Parse(times[0]);
            minutes = Int32.Parse(times[1]);
            seconds = Int32.Parse(times[2]);
            frames = Int32.Parse(times[3]);

            if ((hours >= 24) || (minutes >= 60) || (seconds >= 60) || (frames >= 30))
            {
                throw new FormatException("SMPTE Timecode out of range");
            }
        }

        private static string FormatTimeCodeString(bool minus, int days, int hours, int minutes, int seconds, int frames)
        {
            if (days > 0)
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}:{4:D2}", minus? -days: days, hours, minutes, seconds, frames);
            }
            else
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", minus? -hours: hours, minutes, seconds, frames);
            }
        }

        private static string FormatTimeCodeString(bool minus, int hours, int minutes, int seconds, int frames, bool dropFrame)
        {
            return FormatTimeCodeString(minus, 0, hours, minutes, seconds, frames);
        }

        private static long LTC_23_98_ToTicks(string timeCode)
        {
            int hours, minutes, seconds, frames;

            ParseLTCString(timeCode, out hours, out minutes, out seconds, out frames);

            if (frames >= 24)
            {
                throw new FormatException("Bad LTC timecode format");
            }

            return (long)(10000000L * (1001 / 24000) * (frames + (24 * seconds) + (1440 * minutes) + (86400 * hours)));
        }

        private static long LTC_24_ToTicks(string timeCode)
        {
            int hours, minutes, seconds, frames;

            ParseLTCString(timeCode, out hours, out minutes, out seconds, out frames);

            if (frames >= 24)
            {
                throw new FormatException("Bad LTC timecode format");
            }

            return (long)((10000000L / 24) * (frames + (24 * seconds) + (1440 * minutes) + (86400 * hours)));
        }

        private static long LTC_25_ToTicks(string timeCode)
        {
            int hours, minutes, seconds, frames;

            ParseLTCString(timeCode, out hours, out minutes, out seconds, out frames);

            if (frames >= 25)
            {
                throw new FormatException("Bad LTC 25fps timecode format");
            }

            return (long)((10000000L / 25) * (frames + (25 * seconds) + (1500 * minutes) + (90000 * hours)));
        }

        private static long LTC_30_ToTicks(string timeCode)
        {
            int hours, minutes, seconds, frames;

            ParseLTCString(timeCode, out hours, out minutes, out seconds, out frames);

            if (frames >= 30)
            {
                throw new FormatException("Bad LTC 30fps timecode format");
            }

            return (long)((10000000L / 30) * (frames + (30 * seconds) + (1800 * minutes) + (108000 * hours)));
        }


        private static long Smpte12M_23_98_ToTicks(string timeCode)
        {
            int days, hours, minutes, seconds, frames;

            ParseTimecodeString(timeCode, out days, out hours, out minutes, out seconds, out frames);

            if (frames >= 24)
            {
                throw new FormatException("Bad SMPTE timecode format");
            }

            return (long)(10000000L * (1001 / 24000) * (frames + (24 * seconds) + (1440 * minutes) + (86400 * hours) + (2073600 * days)));
        }

        private static long Smpte12M_24_ToTicks(string timeCode)
        {
            int days, hours, minutes, seconds, frames;

            ParseTimecodeString(timeCode, out days, out hours, out minutes, out seconds, out frames);

            if (frames >= 24)
            {
                throw new FormatException("Bad SMPTE timecode format");
            }

            return (long)((10000000L / 24) * (frames + (24 * seconds) + (1440 * minutes) + (86400 * hours) + (2073600 * days)));
        }

        private static long Smpte12M_25_ToTicks(string timeCode)
        {
            int days, hours, minutes, seconds, frames;

            ParseTimecodeString(timeCode, out days, out hours, out minutes, out seconds, out frames);

            if (frames >= 25)
            {
                throw new FormatException("Bad SMPTE 25fps timecode format");
            }

            return (long)((10000000L / 25) * (frames + (25 * seconds) + (1500 * minutes) + (90000 * hours) + (2160000 * days)));
        }

        private static long Smpte12M_30_ToTicks(string timeCode)
        {
            int days, hours, minutes, seconds, frames;

            ParseTimecodeString(timeCode, out days, out hours, out minutes, out seconds, out frames);

            if (frames >= 30)
            {
                throw new FormatException("Bad SMPTE 30fps timecode format");
            }

            return (long)((10000000L / 30) * (frames + (30 * seconds) + (1800 * minutes) + (108000 * hours) + (2592000 * days)));
        }

        private static string TicksToSMPTE(long ticks, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            switch (rate)
            {
                case SMPTEFrameRate.SMPTERate24fps:
                    return TicksToSmpte12M_24fps(ticks);
                case SMPTEFrameRate.SMPTERate30fps:
                    return TicksToSmpte12M30Fps(ticks);
                default:
                    return TicksToSmpte12M_25fps(ticks); 
            }
        }

        public static long TicksToFrames(long Ticks, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            switch (rate)
            {
                case SMPTEFrameRate.SMPTERate24fps:
                    return (24L * Ticks) / 10000000L;
                case SMPTEFrameRate.SMPTERate30fps:
                    return (30L * Ticks) / 10000000L;
                default:
                    return (25L * Ticks) / 10000000L;
            } 
        }

        public static long FramesToTicks(long frames, SMPTEFrameRate rate = SMPTEFrameRate.SMPTERate25fps)
        {
            switch (rate)
            {
                case SMPTEFrameRate.SMPTERate24fps:
                    return frames * 10000000 / 24L;
                case SMPTEFrameRate.SMPTERate30fps:
                    return frames * 10000000 / 30L;
                default:
                    return frames * 10000000 / 25L;
            }
        }

        private static string TicksToSmpte12M_24fps(long ticks)
        {
            long framecount = TicksToFrames(ticks, SMPTEFrameRate.SMPTERate24fps);
            bool minus = (framecount < 0);
            if (minus)
                framecount = -framecount;
            int days = Convert.ToInt32((framecount / 86400) / 24);
            int hours = Convert.ToInt32((framecount / 86400) % 24);
            int minutes = Convert.ToInt32(((framecount - (86400 * hours)) / 1440) % 60);
            int seconds = Convert.ToInt32(((framecount - (1440 * minutes) - (86400 * hours)) / 24) % 3600);
            int frames = Convert.ToInt32((framecount - (24 * seconds) - (1440 * minutes) - (86400 * hours)) % 24);

            return FormatTimeCodeString(minus, days, hours, minutes, seconds, frames);
        }
        private static string TicksToSmpte12M_25fps(long ticks)
        {
            long framecount = TicksToFrames(ticks, SMPTEFrameRate.SMPTERate25fps);
            bool minus = (framecount < 0);
            if (minus)
                framecount = -framecount;
            int days = Convert.ToInt32((framecount / 90000) / 24);
            int hours = Convert.ToInt32((framecount / 90000) % 24);
            int minutes = Convert.ToInt32(((framecount - (90000 * hours)) / 1500) % 60);
            int seconds = Convert.ToInt32(((framecount - (1500 * minutes) - (90000 * hours)) / 25) % 3600);
            int frames = Convert.ToInt32((framecount - (25 * seconds) - (1500 * minutes) - (90000 * hours)) % 25);

            return FormatTimeCodeString(minus, days, hours, minutes, seconds, frames);
        }

        private static string TicksToSmpte12M30Fps(long ticks)
        {
            long framecount = TicksToFrames(ticks, SMPTEFrameRate.SMPTERate30fps);
            bool minus = (framecount < 0);
            if (minus)
                framecount = -framecount;
            int days = Convert.ToInt32((framecount / 108000) / 24);
            int hours = Convert.ToInt32((framecount / 108000) % 24);
            int minutes = Convert.ToInt32(((framecount - (108000 * hours)) / 1800) % 60);
            int seconds = Convert.ToInt32(((framecount - (1800 * minutes) - (108000 * hours)) / 30) % 3600);
            int frames = Convert.ToInt32((framecount - (30 * seconds) - (1800 * minutes) - (108000 * hours)) % 30);

            return FormatTimeCodeString(minus, days, hours, minutes, seconds, frames);
        }
    }

}
