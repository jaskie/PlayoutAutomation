using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServer: IPlayoutServerProperties, IInitializable, INotifyPropertyChanged
    {
        bool IsConnected { get; }
        IServerDirectory MediaDirectory { get; }
        IAnimationDirectory AnimationDirectory { get; }
        List<IPlayoutServerChannel> Channels { get; }
    }

    public interface IPlayoutServerProperties : IPersistent
    {
        string ServerAddress { get; set; }
        string MediaFolder { get; set; }
        string AnimationFolder { get; set; }
        //IEnumerable<IPlayoutServerChannel> Channels { get; }
    }
}
