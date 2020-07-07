using System;
using System.Diagnostics;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server
{
    public class LocalGpiDeviceBinding : ServerObjectBase, IGpi, IPlugin
    {
        public class GPIPin
        {
            [Hibernate]
            public int Param { get; set; }
            [Hibernate]
            public byte DeviceId { get; set; }
            [Hibernate]
            public int PortNumber { get; set; }
            [Hibernate]
            public byte PinNumber { get; set; }
        }

        internal LocalDevices Owner;      

        public event EventHandler Started;

        public GPIPin Start;        
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
        [Hibernate]
        public bool IsEnabled { get; set; }

        public void ShowAux(int auxNr) { }
        public void HideAux(int auxNr) { }
    }
}
