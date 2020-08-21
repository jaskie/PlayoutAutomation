using TAS.Common;

namespace TAS.Server.VideoSwitch.Model
{
    internal interface IVideoSwitchCommunicator : IRouterCommunicator 
    {
        /// <summary>
        /// Sets transition effect only for next take.
        /// </summary>
        /// <param name="videoSwitchEffect"></param>
        void SetTransitionEffect(VideoSwitchEffect videoSwitchEffect);        
    }
}
