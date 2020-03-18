using System;
using System.Collections.Generic;

namespace TAS.Client.XKeys
{
    public class KeyNotifyEventArgs: EventArgs
    {
        public XKeysDevice Device { get; }

        public int Key { get; }

        public bool IsPressed { get; }

        public IReadOnlyList<int> AllKeys { get; }

        public KeyNotifyEventArgs(XKeysDevice device, int key, bool isPressed, IReadOnlyList<int> allKeys)
        {
            Device = device;
            Key = key;
            IsPressed = isPressed;
            AllKeys = allKeys;
        }
    }
}
