using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TAS.Common
{
    [DebuggerDisplay("{Num}/{Den}")]
    public struct RationalNumber : IEquatable<RationalNumber>
    {
        private readonly long _num;
        private readonly long _den;

        public RationalNumber(long numerator, long denominator)
        {
            this._num = numerator;
            this._den = denominator;
        }

        public long Num { get { return _num; } }
        public long Den { get { return _den; } }

        public bool IsZero
        {
            get { return _num == 0; }
        }

        public bool IsInvalid
        {
            get { return _den == 0 && _num != 0; }
        }

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

    }   
}
