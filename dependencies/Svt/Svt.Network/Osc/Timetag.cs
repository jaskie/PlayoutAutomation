using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Network.Osc
{
    public struct Timetag
    {
        public UInt64 Tag;

        public DateTime Timestamp
        {
            get
            {
                return Utils.TimetagToDateTime(Tag);
            }
            set
            {
                Tag = Utils.DateTimeToTimetag(value);
            }
        }

        /// <summary>
        /// Gets or sets the fraction of a second in the timestamp. the double precision number is multiplied by 2^32
        /// giving an accuracy down to about 230 picoseconds ( 1/(2^32) of a second)
        /// </summary>
        public double Fraction
        {
            get
            {
                return Utils.TimetagToFraction(Tag);
            }
            set
            {
                Tag = (Tag & 0xFFFFFFFF00000000) + (UInt32)(value * 0xFFFFFFFF);
            }
        }

        public Timetag(UInt64 value)
        {
            this.Tag = value;
        }

        public Timetag(DateTime value)
        {
            Tag = 0;
            this.Timestamp = value;
        }

        public override bool Equals(System.Object obj)
        {
            if (obj.GetType() == typeof(Timetag))
            {
                if (this.Tag == ((Timetag)obj).Tag)
                    return true;
                else
                    return false;
            }
            else if (obj.GetType() == typeof(UInt64))
            {
                if (this.Tag == ((UInt64)obj))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public static bool operator ==(Timetag a, Timetag b)
        {
            if (a.Equals(b))
                return true;
            else
                return false;
        }

        public static bool operator !=(Timetag a, Timetag b)
        {
            if (a.Equals(b))
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return (int)(((uint)(Tag >> 32) + (uint)(Tag & 0x00000000FFFFFFFF)) / 2);
        }
    }
}