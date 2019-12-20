using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces
{
    public interface IPreview: INotifyPropertyChanged
    {
        IPlayoutServerChannel Channel { get; }
        void LoadMovie(IMedia media, long seek, long duration, long position, double audioLevel);
        void UnloadMovie();
        void PlayLiveDevice();
        void LoadStillImage(IMedia media, VideoLayer layer);
        bool UnLoadStillImage(VideoLayer layer);
        IMedia LoadedMovie { get; }
        TVideoFormat VideoFormat { get; }
        void Pause();
        void Play();
        bool IsConnected { get; }
        bool IsMovieLoaded { get; }
        bool IsPlaying { get; }
        bool HaveLiveDevice { get; }
        bool IsLivePlaying { get; }
        long MoviePosition { get; set; }
        long MovieSeekOnLoad { get; }
        double AudioVolume { get; set; }
        Dictionary<VideoLayer, IMedia> LoadedStillImages { get; }
        event EventHandler<MediaOnLayerEventArgs> StillImageLoaded;
        event EventHandler<MediaOnLayerEventArgs> StillImageUnLoaded;
    }
}
