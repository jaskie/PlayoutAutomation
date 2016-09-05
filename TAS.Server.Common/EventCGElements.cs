using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class EventCGElements: Remoting.Server.DtoBase, IEventCGElementsState
    {
        public static readonly UInt64 Mask = 0xFFFF; // 2 bits left

        bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { SetField(ref _isEnabled, value, nameof(IsEnabled)); } }
        bool _parentalVisible;
        public bool ParentalVisible { get { return _parentalVisible; } set { SetField(ref _parentalVisible, value, nameof(ParentalVisible)); } }
        byte _parental;
        public byte Parental { get { return _parental; } set { SetField(ref _parental, value, nameof(Parental)); } }
        bool _logoVisible;
        public bool LogoVisible { get { return _logoVisible; } set { SetField(ref _logoVisible, value, nameof(LogoVisible)); } }
        byte _logo;
        public byte Logo { get { return _logo; } set { SetField(ref _logo, value, nameof(Logo)); } }
        bool _crawlVisible;
        public bool CrawlVisible { get { return _crawlVisible; } set { SetField(ref _crawlVisible, value, nameof(CrawlVisible)); } }
        byte _crawl;
        public byte Crawl { get { return _crawl; }  set { SetField(ref _crawl, value, nameof(Crawl)); } }
        public ulong ToUInt64()
        {
            return Convert.ToUInt64(IsEnabled)
                 | (ulong)(ParentalVisible ? ((Parental+1) & 0xF): 0L) << 2 // 4 bits, 2-5
                 | (ulong)(LogoVisible ? ((Logo+1)& 0xF): 0L) << 6 // 4 bits, 6-9
                 | (ulong)(CrawlVisible ? ((Crawl+1) & 0xF): 0L) << 10 // 4 bits, 10-14
                 ;
        }

        public EventCGElements()
        {

        }

        public EventCGElements(ICGElementsState other)
        {
            _isEnabled = other.IsEnabled;
            _parentalVisible = other.ParentalVisible;
            _parental = other.Parental;
            _logoVisible = other.LogoVisible;
            _logo = other.Logo;
            _crawlVisible = other.CrawlVisible;
            _crawl = other.Crawl;
        }
        public EventCGElements(ulong initialValue)
        {
            IsEnabled = (initialValue & 0x1) > 0;
            var value = ((initialValue >> 2) & 0xF);
            if (value == 0)
            {
                _parentalVisible = false;
                _parental = 0;
            }
            else
            {
                _parentalVisible = true;
                _parental = (byte)(value - 1);
            }
            value = ((initialValue >> 6) & 0xF);
            if (value == 0)
            {
                _logoVisible = false;
                _logo = 0;
            }
            else
            {
                _logoVisible = true;
                _logo = (byte)(value - 1);
            }

            value = ((initialValue >> 10) & 0xF);
            if (value == 0)
            {
                _crawlVisible = false;
                _crawl = 0;
            }
            else
            {
                _crawlVisible = true;
                _crawl = (byte)(value - 1);
            }
        }

    }
}
