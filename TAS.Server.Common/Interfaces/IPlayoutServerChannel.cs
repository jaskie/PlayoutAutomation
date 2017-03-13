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
        IPlayoutServer OwnerServer { get; }

    }

    public interface IPlayoutServerChannelProperties
    {
        int Id { get; }
        string ChannelName { get; set; }
        decimal MasterVolume { get; set; }
        string LiveDevice { get; set; }
    }
}
