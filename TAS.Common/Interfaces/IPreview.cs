using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IPreview: INotifyPropertyChanged
    {
        void PreviewLoad(IMedia media, long seek, long duration, long position, double audioLevel);
        IMedia PreviewMedia { get; }
        IPlayoutServerChannel PlayoutChannelPRV { get; }
        VideoFormatDescription FormatDescription { get; }
        void PreviewPause();
        void PreviewPlay();
        void PreviewUnload();
        bool PreviewLoaded { get; }
        bool PreviewIsPlaying { get; }
        long PreviewPosition { get; set; }
        long PreviewSeek { get; }
        double PreviewAudioVolume { get; set; }
    }
}
