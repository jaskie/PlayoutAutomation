using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServerChannel: IPlayoutServerChannelProperties, INotifyPropertyChanged
    {
        bool IsServerConnected { get; }
        int  AudioLevel { get; } 
    }

    public interface IPlayoutServerChannelProperties
    {
        int Id { get; }
        string ChannelName { get; set; }
        decimal MasterVolume { get; set; }
        string LiveDevice { get; set; }
        TVideoFormat VideoFormat { get; }
    }
}
