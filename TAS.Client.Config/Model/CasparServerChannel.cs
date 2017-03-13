using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparServerChannel: IPlayoutServerChannelProperties
    {
        public CasparServerChannel()
        {
            MasterVolume = 1m;
        }
        public int Id { get; set; }
        public string ChannelName { get; set; }
        public decimal MasterVolume { get; set; } 
        public string LiveDevice { get; set; }
        
        internal object Owner;
        public override string ToString()
        {
            return string.Format("{0} - {1}", Owner, ChannelName);
        }
    }
}
