using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServerChannel: IPlayoutServerChannelProperties, IInitializable, INotifyPropertyChanged
    {
        IPlayoutServer OwnerServer { get; set; }
    }

    public interface IPlayoutServerChannelProperties
    {
        string ChannelName { get; set; }
        int ChannelNumber { get; set; }
        decimal MasterVolume { get; set; }
        string LiveDevice { get; set; }
    }
}
