using System;

namespace TAS.Server.Advantech.Model.Args
{
    public class GpiStateChangedEventArgs : EventArgs
    {
        public byte DeviceId { get; }
        public byte Pin { get; }
        public byte Port { get; }
        public bool NewState { get; }
        public GpiStateChangedEventArgs(byte deviceId, byte port, byte pin, bool newState)
        {
            DeviceId = deviceId;
            Port = port;
            Pin = pin;
            NewState = newState;
        }
    }
}
