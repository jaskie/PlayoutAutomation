using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServer: IPlayoutServerProperties, INotifyPropertyChanged
    {
        bool IsConnected { get; }
        IServerDirectory MediaDirectory { get; }
        IAnimationDirectory AnimationDirectory { get; }
        List<IPlayoutServerChannel> Channels { get; }
        List<IRecorder> Recorders { get; }
    }

    public interface IPlayoutServerProperties : IPersistent
    {
        string ServerAddress { get; set; }
        string MediaFolder { get; set; }
        string AnimationFolder { get; set; }
        TServerType ServerType { get; }
    }
}
