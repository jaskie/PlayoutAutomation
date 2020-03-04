using System;
using System.Diagnostics;
using System.Xml.Serialization;
using jNet.RPC.Server;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class LocalGpiDeviceBinding : DtoBase, IGpi, IEnginePlugin
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

        internal IEngine Engine;

        [XmlAttribute]
        public string EngineName { get; set; }

        public event EventHandler Started;

        public GPIPin Start;
        public GPIPin[] Logos;
        public GPIPin[] Crawls;
        public GPIPin[] Parentals;
        public GPIPin WideScreen;

        private void _actionCheckAndExecute(EventHandler handler, GPIPin pin, byte deviceId, byte port, byte bit)
        {
            if (pin == null || deviceId != pin.DeviceId || port != pin.PortNumber || bit != pin.PinNumber)
                return;
            Debug.WriteLine("Advantech device {0} notification port {1} bit {2}", deviceId, port, bit);
            handler?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifyChange(byte deviceId, byte port, byte bit, bool newValue)
        {
            if (newValue)
                _actionCheckAndExecute(Started, Start, deviceId, port, bit);
        }
                
        bool _isWideScreen;
        public bool IsWideScreen
        {
            get => _isWideScreen;

            set
            {
                if (!SetField(ref _isWideScreen, value))
                    return;
                _isWideScreen = value;
                var pin = WideScreen;
                if (pin != null)
                    Owner.SetPortState(pin.DeviceId, pin.PortNumber, pin.PinNumber, value);
            }
        }

        public void ShowAux(int auxNr) { }
        public void HideAux(int auxNr) { }
    }
}
