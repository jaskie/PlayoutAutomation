using System.Threading.Tasks;
using TAS.Common;

namespace TAS.Server.VideoSwitch.Model.Interfaces
{
    internal interface IVideoSwitchCommunicator : IRouterCommunicator
    {
        Task Preload(int sourceId);     
        void SetTransitionStyle(VideoSwitcherTransitionStyle mixEffect);
        void SetMixSpeed(double rate);
        Task Take();
    }
}
