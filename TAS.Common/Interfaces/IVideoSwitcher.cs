using System.Threading.Tasks;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitcher : IVideoSwitch
    {
        Task PreloadSource(int sourceId);
        void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect);        
        VideoSwitcherTransitionStyle DefaultEffect { get; }
        Task Take();
    }
}
