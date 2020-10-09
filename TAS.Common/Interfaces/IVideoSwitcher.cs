using System.Threading.Tasks;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitcher : IRouter
    {
        Task PreloadSource(int sourceId);
        void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect);        
        VideoSwitcherTransitionStyle DefaultEffect { get; }
        Task Take();
    }
}
