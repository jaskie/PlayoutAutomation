using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class EventCGElements: Remoting.Server.DtoBase, ICGElementsState
    {
        public static readonly UInt64 Mask = 0xFFFF; // 2 bits left

        bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { SetField(ref _isEnabled, value, nameof(IsEnabled)); } }
        byte _parental;
        public byte Parental { get { return _parental; } set { SetField(ref _parental, value, nameof(Parental)); } }
        byte _logo;
        public byte Logo { get { return _logo; } set { SetField(ref _logo, value, nameof(Logo)); } }
        byte _crawl;
        public byte Crawl { get { return _crawl; }  set { SetField(ref _crawl, value, nameof(Crawl)); } }
        public ulong ToUInt64()
        {
            return Convert.ToUInt64(IsEnabled)
                 | (ulong)(Parental & 0xF) << 2 // 4 bits, 2-5
                 | (ulong)(Logo & 0xF) << 6 // 4 bits, 6-9
                 | (ulong)(Crawl & 0xF) << 10 // 4 bits, 10-14
                 ;
        }

        public EventCGElements()
        {

        }

        public EventCGElements(ICGElementsState other)
        {
            _isEnabled = other.IsEnabled;
            _parental = other.Parental;
            _logo = other.Logo;
            _crawl = other.Crawl;
        }
        public EventCGElements(ulong initialValue)
        {
            IsEnabled = (initialValue & 0x1) > 0;
            _parental = (byte)((initialValue >> 2) & 0xF);
            _logo = (byte)((initialValue >> 6) & 0xF);
            _crawl = (byte)((initialValue >> 10) & 0xF);
        }

    }
}
