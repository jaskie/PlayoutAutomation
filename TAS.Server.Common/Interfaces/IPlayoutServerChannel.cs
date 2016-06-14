using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServerChannel: Remoting.IDto, IPlayoutServerChannelConfig, IInitializable, INotifyPropertyChanged
    {
        IPlayoutServer OwnerServer { get; set; }
    }
}
