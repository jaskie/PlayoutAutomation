using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Svt.Network.Osc
{
    public class OscPacketEventArgs: EventArgs
    {
        public OscPacketEventArgs(OscPacket packet, IPAddress sourceAddress)
        {
            Packet = packet;
            SourceAddress = sourceAddress;
        }
        public OscPacket Packet { get; private set; }
        public IPAddress SourceAddress { get; private set; }
    }

}
