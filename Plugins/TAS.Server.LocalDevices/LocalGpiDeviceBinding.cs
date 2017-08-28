using System;
using System.Diagnostics;
using System.Xml.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class LocalGpiDeviceBinding : Remoting.Server.DtoBase, IGpi
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
        }

        internal LocalDevices Owner;

        [XmlAttribute]
        public ulong IdEngine;

        public event EventHandler Started;

        public GPIPin Start;
        public GPIPin[] Logos;
        public GPIPin[] Crawls;
        public GPIPin[] Parentals;
        public GPIPin WideScreen;

        void _actionCheckAndExecute(EventHandler handler, GPIPin pin, byte deviceId, byte port, byte bit)
        {
            if (pin != null && deviceId == pin.DeviceId && port == pin.PortNumber && bit == pin.PinNumber)
            {
                Debug.WriteLine("Advantech device {0} notification port {1} bit {2}", deviceId, port, bit);
                handler?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void NotifyChange(byte deviceId, byte port, byte bit, bool newValue)
        {
            if (newValue)
                _actionCheckAndExecute(Started, Start, deviceId, port, bit);
        }
                
        bool _isWideScreen;
        public bool IsWideScreen
        {
            get
            {
                return _isWideScreen;
            }

            set
            {
                if (SetField(ref _isWideScreen, value))
                {
                    _isWideScreen = value;
                    var pin = WideScreen;
                    if (pin != null)
                        Owner.SetPortState(pin.DeviceId, pin.PortNumber, pin.PinNumber, value);
                }
            }
        }

        public void ShowAux(int auxNr) { }
        public void HideAux(int auxNr) { }
    }
}
