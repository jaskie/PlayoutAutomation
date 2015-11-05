using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServer: IPlayoutServerConfig, IInitializable, INotifyPropertyChanged
    {
        bool IsConnected { get; }
        IServerDirectory MediaDirectory { get; }
        IAnimationDirectory AnimationDirectory { get; }
        List<IPlayoutServerChannel> Channels { get; }
    }
}
