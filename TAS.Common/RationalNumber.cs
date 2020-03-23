using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace TAS.Common
{
    [DebuggerDisplay("{Num}/{Den}")]
    public struct RationalNumber : IEquatable<RationalNumber>
    {
        public RationalNumber(long num, long den)
        {
            Num = num;
            Den = den;
        }
        public long Num { get; }
        public long Den { get; }

        [IgnoreDataMember]
        public bool IsZero => Num == 0;

        [IgnoreDataMember]
        public bool IsInvalid => Den == 0 && Num != 0;

        public bool Equals(RationalNumber r)
        {
            if (r.IsZero && IsZero)
                return true;
            if (r.IsInvalid && IsInvalid)
                return true;
            return Num * r.Den == Den * r.Num;
        }

        public override bool Equals(object o)
        {
            if (!(o is RationalNumber))
                return false;
            return Equals((RationalNumber)o);
        }

        public override int GetHashCode()
        {
            if (IsZero)
                return 0;
            if (IsInvalid)
                return int.MinValue;
            return ((float)Num / Den).GetHashCode();
        }

        public static RationalNumber Zero = new RationalNumber(0, 1);

        public static RationalNumber One = new RationalNumber(1, 1);

        public override string ToString()
        {
            return $"{Num}/{Den}";
        }

        #region Operators

        public static bool operator == (RationalNumber r1, RationalNumber r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator != (RationalNumber r1, RationalNumber r2)
        {
            return !r1.Equals(r2);
        }

        public static RationalNumber operator /(RationalNumber r1, RationalNumber r2)
        {
            if (r1.IsZero)
            {
                return Zero;
            }
            if (r2.IsZero)
            {
                throw new DivideByZeroException();
            }
            return new RationalNumber(r1.Num * r2.Den,
                                r1.Den * r2.Num);
        }
        public static RationalNumber operator *(RationalNumber r1, RationalNumber r2)
        {
            if (r1.IsZero || r2.IsZero)
            {
                return Zero;
            }
            return new RationalNumber(r1.Num * r2.Num,
                                r1.Den * r2.Den);
        }

        public static implicit operator double(RationalNumber value)
        {
            return (double)value.Num / value.Den;
        }

        public static implicit operator long(RationalNumber value)
        {
            return value.Num / value.Den;
        }

        public static implicit operator int(RationalNumber value)
        {
            return (int)(value.Num / value.Den);
        }

        #endregion // Operators



    }
}
