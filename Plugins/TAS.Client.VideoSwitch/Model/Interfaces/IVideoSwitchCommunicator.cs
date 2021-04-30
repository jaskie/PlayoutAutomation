using TAS.Common;

namespace TAS.Server.VideoSwitch.Model.Interfaces
{
    internal interface IVideoSwitchCommunicator : IRouterCommunicator
    {
        void Preload(int sourceId);     
        void SetTransitionStyle(VideoSwitcherTransitionStyle mixEffect);
        void SetMixSpeed(double rate);
        void Take();
    }
}
