using System;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.Advantech
{
    public class GpiBinding : ServerObjectBase, IGpi, IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();        
        
        public GpiPin Start;               

        private void _actionCheckAndExecute(EventHandler handler, GpiPin pin, byte deviceId, byte port, byte bit)
        {
            if (pin == null || deviceId != pin.DeviceId || port != pin.PortNumber || bit != pin.PinNumber)
                return;
            Logger.Debug("Advantech device {0} notification port {1} bit {2}", deviceId, port, bit);
            handler?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifyChange(byte deviceId, byte port, byte bit, bool newValue)
        {
            if (!newValue)
                return;
                        
            _actionCheckAndExecute(Started, Start, deviceId, port, bit);
        }
                       
        [Hibernate]
        public bool IsEnabled { get; set; }
        public event EventHandler Started;
    }
}
