using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IPlayoutServerChannel: IPlayoutServerChannelConfig, IInitializable, INotifyPropertyChanged
    {
        IPlayoutServer OwnerServer { get; }
        IEngine Engine { get; set; }
        void ReStart(VideoLayer aVideoLayer);

        bool Load(IEvent aEvent);
        bool Load(IServerMedia media, VideoLayer videolayer, long seek, long duration);
        bool Load(System.Drawing.Color color, VideoLayer layer);
        bool Seek(VideoLayer videolayer, long position);
        bool LoadNext(IEvent aEvent);
        bool Play(IEvent aEvent);
        bool Play(VideoLayer videolayer);
        bool Stop(IEvent aEvent);
        bool Stop(VideoLayer videolayer);
        bool Pause(IEvent aEvent);
        bool Pause(VideoLayer videolayer);
        void Clear(VideoLayer aVideoLayer);
        void Clear();
        void SetVolume(VideoLayer videolayer, decimal volume);
        void SetAspect(VideoLayer layer, bool narrow);
    }
}
