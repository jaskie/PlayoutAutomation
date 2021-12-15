using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.VideoSwitch.Model
{
    public abstract class VideoSwitcher : VideoSwitchBase, IVideoSwitcher
    {
        #region Configuration
        [Hibernate]
        public PortInfo GpiPort { get; set; }

        [Hibernate]
        public VideoSwitcherTransitionStyle DefaultEffect { get; set; } = VideoSwitcherTransitionStyle.Cut;
        #endregion

        protected VideoSwitcher(int defaultPort) : base(defaultPort)
        {
        }

        public abstract void PreloadSource(int sourceId);

        public abstract void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect);

        public abstract void Take();
        
    }
}
