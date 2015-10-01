using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServerChannelConfig
    {
        string ChannelName { get; set; }
        int ChannelNumber { get; set; }
        decimal MasterVolume { get; set; }
        string LiveDevice { get; set; }
    }
}
