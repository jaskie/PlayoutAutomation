namespace TAS.Common.Interfaces
{
    public interface IVideoSwitcher : IRouter
    {
        void PreloadSource();
        void SetMixEffect(VideoSwitchEffect videoSwitchEffect);
        VideoSwitchEffect DefaultEffect { get; }
        void Take();
    }
}
