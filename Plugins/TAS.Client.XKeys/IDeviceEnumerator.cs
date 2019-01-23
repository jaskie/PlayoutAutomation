using System;

namespace TAS.Client.XKeys
{
    internal interface IDeviceEnumerator
    {
        void SetBacklight(byte unitId, int keyNr, BacklightColorEnum color, bool blinking);
        event EventHandler<KeyNotifyEventArgs> KeyNotified;
        event EventHandler<DeviceEventArgs> DeviceConnected;
    }
}