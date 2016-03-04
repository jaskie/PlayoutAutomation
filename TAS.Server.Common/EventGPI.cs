using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Common
{
    public struct EventGPI
    {
        public bool CanTrigger;
        public TParental Parental;
        public TLogo Logo;
        public TCrawl Crawl;
        public UInt64 ToUInt64()
        {
            return Convert.ToUInt64(CanTrigger)
                 | ((UInt64)Parental & 0xF) << 2 // 4 bits, 2-5
                 | ((UInt64)Logo & 0xF) << 6 // 4 bits, 6-9
                 | ((UInt64)Crawl & 0xF) << 10 // 4 bits, 10-14
                 ;
        }
        public static EventGPI FromUInt64(UInt64 value)
        {
            EventGPI gpiValue;
            gpiValue.CanTrigger = (value & 0x1) > 0;
            gpiValue.Parental = (TParental)((value >> 2) & 0xF);
            gpiValue.Logo = (TLogo)((value >> 6) & 0xF);
            gpiValue.Crawl = (TCrawl)((value >> 10) & 0xF);
            return gpiValue;
        }
        public static readonly UInt64 Mask = 0xFFFF; // 2 bits more
    }
}
