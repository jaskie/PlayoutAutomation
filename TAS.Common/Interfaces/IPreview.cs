using System;
using System.ComponentModel;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces
{
    public interface IPreview: INotifyPropertyChanged
    {
        IPlayoutServerChannel Channel { get; }
        void LoadMovie(IMedia media, long seek, long duration, long position, double audioLevel);
        void LoadStill(IMedia media, VideoLayer layer);
        IMedia Media { get; }
        VideoFormatDescription FormatDescription { get; }
        void Pause();
        void Play();
        void Unload();
        bool IsConnected { get; }
        bool IsLoaded { get; }
        bool IsPlaying { get; }
        long Position { get; set; }
        long LoadedSeek { get; }
        double AudioVolume { get; set; }
        event EventHandler<MediaOnLayerEventArgs> StillLoaded;
    }
}
