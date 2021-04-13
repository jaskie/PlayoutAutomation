using TAS.Common;

namespace TAS.Server.VideoSwitch.Model
{
    public class TransitionEffect
    {       
        public VideoSwitcherTransitionStyle Type { get; }
        
        public TransitionEffect(VideoSwitcherTransitionStyle type)
        {
            Type = type;
        }        
    }
}
