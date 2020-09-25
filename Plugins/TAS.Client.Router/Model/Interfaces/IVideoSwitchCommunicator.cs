using TAS.Common;

namespace TAS.Server.VideoSwitch.Model.Interfaces
{
    internal interface IVideoSwitchCommunicator : IRouterCommunicator
    {
        void PreloadSource();
        void SetMixEffect(VideoSwitchEffect mixEffect);
        VideoSwitchEffect DefaultEffect { get; }
        void Take();
    }
}
