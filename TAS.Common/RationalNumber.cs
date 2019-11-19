using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace TAS.Common
{
    [DebuggerDisplay("{Num}/{Den}")]
    [DataContract]
    public struct RationalNumber : IEquatable<RationalNumber>
    {
        [DataMember(Name ="Num")]
        private readonly long _num;
        [DataMember(Name ="Den")]
        private readonly long _den;

        public RationalNumber(long numerator, long denominator)
        {
            _num = numerator;
            _den = denominator;
        }
        public long Num => _num;
        public long Den => _den;

        public bool IsZero => _num == 0;

        public bool IsInvalid => _den == 0 && _num != 0;

        public bool Equals(RationalNumber r)
        {
            if (r.IsZero && IsZero)
                return true;
            if (r.IsInvalid && IsInvalid)
                return true;
            return _num * r._den == _den * r._num;
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
                return Int32.MinValue;
            return ((float)_num / _den).GetHashCode();
        }

        public static RationalNumber Zero = new RationalNumber(0, 1);

        public override string ToString()
        {
            return $"{_num}/{_den}";
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
