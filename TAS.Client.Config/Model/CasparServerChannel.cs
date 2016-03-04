using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparServerChannel: IPlayoutServerChannel
    {
        public CasparServerChannel()
        {
            MasterVolume = 1m;
        }
        public string ChannelName { get; set; }
        public int ChannelNumber { get; set; }
        public decimal MasterVolume { get; set; } 
        public string LiveDevice { get; set; }

        public IPlayoutServer OwnerServer { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return string.Format("{0} - {1}", OwnerServer, ChannelName);
        }

        #region not implemented
        public void ReStart(IEvent ev)
        {
            throw new NotImplementedException();
        }

        public bool Load(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Load(IServerMedia media, VideoLayer videolayer, long seek, long duration)
        {
            throw new NotImplementedException();
        }

        public bool Load(Color color, VideoLayer layer)
        {
            throw new NotImplementedException();
        }

        public bool Seek(VideoLayer videolayer, long position)
        {
            throw new NotImplementedException();
        }

        public bool LoadNext(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Play(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Play(VideoLayer videolayer)
        {
            throw new NotImplementedException();
        }

        public bool Stop(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Stop(VideoLayer videolayer)
        {
            throw new NotImplementedException();
        }

        public bool Pause(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Pause(VideoLayer videolayer)
        {
            throw new NotImplementedException();
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void SetVolume(VideoLayer videolayer, decimal volume)
        {
            throw new NotImplementedException();
        }

        public void SetAspect(VideoLayer layer, bool narrow)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
