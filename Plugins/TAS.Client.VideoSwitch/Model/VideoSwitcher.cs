﻿using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public abstract class VideoSwitcher : RouterBase, IVideoSwitcher
    {
        #region Configuration
        [Hibernate]
        public PortInfo GpiPort { get; set; }

        [Hibernate]
        public VideoSwitcherTransitionStyle DefaultEffect { get; set; } = VideoSwitcherTransitionStyle.Cut;
        #endregion

        protected override void Communicator_SourceChanged(object sender, EventArgs<CrosspointInfo> e)
        {
            if (e.Value.InPort == GpiPort?.Id)
                RaiseGpiStarted();

            SelectedSource = Sources.FirstOrDefault(param => param.Id == e.Value.InPort);
        }
        
        public void PreloadSource(int sourceId)
        {
            if (!(Communicator is IVideoSwitchCommunicator videoSwitch))
                return;
            videoSwitch.Preload(sourceId);
        }

        public void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect)
        {
            if (!(Communicator is IVideoSwitchCommunicator videoSwitch))
                return;
            
            videoSwitch.SetTransitionStyle(videoSwitchEffect);
        }        

        public void Take()
        {
            if (!(Communicator is IVideoSwitchCommunicator videoSwitch))
                return;
            videoSwitch.Take();
        }        

    }
}
