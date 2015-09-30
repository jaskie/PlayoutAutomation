using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup.Model
{
    public class CasparServerChannel: IPlayoutServerChannel
    {
        public CasparServerChannel()
        {
            MasterVolume = 1m;
        }
        public string ChannelName { get; set; }
        public int ChannelNumber { get; set; }
        public decimal MasterVolume { get; set; } 
        public string LiveDevice { get; set; }
    }
}
