using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Server.Common.Interfaces
{
    public interface IPlayoutServer: IPlayoutServerProperties, INotifyPropertyChanged
    {
        bool IsConnected { get; }
        IServerDirectory MediaDirectory { get; }
        IAnimationDirectory AnimationDirectory { get; }
        IEnumerable<IPlayoutServerChannel> Channels { get; }
        IEnumerable<IRecorder> Recorders { get; }
    }

    public interface IPlayoutServerProperties : IPersistent
    {
        string ServerAddress { get; set; }
        int OscPort { get; set; }
        string MediaFolder { get; set; }
        string AnimationFolder { get; set; }
        TServerType ServerType { get; }
    }
}
