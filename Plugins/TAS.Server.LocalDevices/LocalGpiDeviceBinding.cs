using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class LocalGpiDeviceBinding : IGpi
    {

        public class GPIPin
        {
            [XmlAttribute]
            public int Param;
            [XmlAttribute]
            public byte DeviceId;
            [XmlAttribute]
            public int PortNumber;
            [XmlAttribute]
            public byte PinNumber;
        }   internal LocalDevices Owner;

        [XmlAttribute]
        public UInt64 IdEngine;

        public event Action Started;

        public GPIPin Start;
        public GPIPin[] Logos;
        public GPIPin[] Crawls;
        public GPIPin[] Parentals;

        void _actionCheckAndExecute(Action action, GPIPin pin, byte deviceId, byte port, byte bit)
        {
            if (action != null && pin != null && deviceId == pin.DeviceId && port == pin.PortNumber && bit == pin.PinNumber)
                action();
        }

        internal void NotifyChange(byte deviceId, byte port, byte bit, bool newValue)
        {
            var pin = Start;
            if (newValue)
                _actionCheckAndExecute(Started, Start, deviceId, port, bit);
        }

        int _parental;
        [XmlIgnore]
        public int Parental
        {
            get { return _parental; }
            set
            {
                if (_parental != value)
                {
                    _parental = value;
                    _setSinglePin(Parentals, value);
                }
            }
        }

        int _logo;
        [XmlIgnore]
        public int Logo
        {
            get { return _logo; }
            set
            {
                if (_logo != value)
                {
                    _logo = value;
                    _setSinglePin(Logos, value);
                }
            }
        }

        int _crawl;
        [XmlIgnore]
        public int Crawl
        {
            get { return _crawl; }
            set
            {
                if (_crawl != value)
                {
                    _crawl = value;
                    _setSinglePin(Crawls, value);
                }
            }
        }

        [XmlIgnore]
        public bool CrawlVisible
        {
            get { return _crawl != 0; }
            set
            {
                if (_crawl != 0)
                {
                    _crawl = 0;
                    _setSinglePin(Crawls, 0);
                }
            }
        }
        private List<int> _visibleAuxes = new List<int>();
        [XmlIgnore]
        public int[] VisibleAuxes { get { return _visibleAuxes.ToArray(); } }

        public void ShowAux(int auxNr) { }
        public void HideAux(int auxNr) { }

        void _setSinglePin(GPIPin[] pins, int value)
        {
            var owner = Owner;
            if (pins != null && owner != null)
            {
                pins.Where(p => p.Param != value).ToList().ForEach(p => owner.SetPortState(p.DeviceId, p.PortNumber, p.PinNumber, false));
                pins.Where(p => p.Param == value).ToList().ForEach(p => owner.SetPortState(p.DeviceId, p.PortNumber, p.PinNumber, true));
            }
        }

    }
}
