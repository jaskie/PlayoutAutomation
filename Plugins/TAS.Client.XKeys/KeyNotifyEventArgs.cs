using System;
using System.Collections.Generic;

namespace TAS.Client.XKeys
{
    public class KeyNotifyEventArgs: EventArgs
    {
        public byte UnitId { get; }

        public int Key { get; }

        public bool IsPressed { get; }

        public IReadOnlyList<int> AllKeys { get; }

        public KeyNotifyEventArgs(byte unitId, int key, bool isPressed, IReadOnlyList<int> allKeys)
        {
            UnitId = unitId;
            Key = key;
            IsPressed = isPressed;
            AllKeys = allKeys;
        }
    }
}
