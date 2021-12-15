namespace TAS.Common.Interfaces
{
    public interface IVideoSwitcher : IVideoSwitch
    {
        void PreloadSource(int sourceId);
        void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect);
        VideoSwitcherTransitionStyle DefaultEffect { get; }
        void Take();
    }
}
