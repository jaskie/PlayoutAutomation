using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Network.Osc
{
    public class OscPacketEventArgs: EventArgs
    {
        public OscPacketEventArgs(OscPacket p)
        {
            Packet = p;
        }
        public OscPacket Packet { get; private set; }
    }

    public class OscBytesEventArgs: EventArgs
    {
        public OscBytesEventArgs(byte[] bytes)
        {
            Bytes = bytes;
        }
        public byte[] Bytes { get; private set; }
    }
}
