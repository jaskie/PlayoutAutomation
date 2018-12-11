using System;

namespace TAS.Client.XKeys
{
    public class KeyNotifyEventArgs: EventArgs
    {
        public byte UnitId { get; }

        public int Key { get; }

        public bool IsPressed { get; }

        public KeyNotifyEventArgs(byte unitId, int key, bool isPressed)
        {
            UnitId = unitId;
            Key = key;
            IsPressed = isPressed;
        }
    }
}
