using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ComponentModelRPC.Server;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Server.Media;

namespace TAS.Server
{
    public class Preview: DtoBase, IPreview
    {
        private const int PerviewPositionSetDelay = 100;

        private readonly Engine _engine;

        [JsonProperty(nameof(LoadedMovie))]
        private IMedia _loadedMovie;
        private long _duration;
        private long _position;
        private double _audioVolume;
        private bool _isLoaded;
        private bool _isPlaying;
        private long _currentTicks;
        private CancellationTokenSource _previewPositionCancellationTokenSource;
        private long _previewLastPositionSetTick;
        private readonly ConcurrentDictionary<VideoLayer, IMedia> _previewLoadedStills = new ConcurrentDictionary<VideoLayer, IMedia>();
        private readonly CasparServerChannel _channel;

        public Preview(Engine engine, CasparServerChannel previewChannel)
        {
            _engine = engine;
            _channel = previewChannel;
            previewChannel.PropertyChanged += ChannelPropertyChanged;
        }

        [XmlIgnore]
        public IMedia LoadedMovie => _loadedMovie;

        [JsonProperty]
        public IPlayoutServerChannel Channel => _channel;

        [JsonProperty]
        public bool IsConnected => _channel.IsServerConnected;

        [JsonProperty]
        public TVideoFormat VideoFormat => _engine.VideoFormat; 

        [XmlIgnore, JsonProperty]
        public long MovieSeekOnLoad { get; private set; }

        [XmlIgnore]
        [JsonProperty]
        public long MoviePosition // from 0 to duration
        {
            get => _position;
            set
            {
                if (_channel == null || _loadedMovie == null)
                    return;
                if (_isPlaying)
                    Pause();
                long newSeek = value < 0 ? 0 : value;
                long maxSeek = _duration - 1;
                if (newSeek > maxSeek)
                    newSeek = maxSeek;
                if (SetField(ref _position, newSeek))
                {
                    _previewPositionCancellationTokenSource?.Cancel();
                    var cancellationTokenSource = new CancellationTokenSource();
                    Task.Run(() =>
                    {
                        Thread.Sleep(PerviewPositionSetDelay);
                        if (!cancellationTokenSource.IsCancellationRequested ||
                            _currentTicks > _previewLastPositionSetTick + TimeSpan.TicksPerMillisecond * PerviewPositionSetDelay * 3)
                        {
                            _previewLastPositionSetTick = _currentTicks;
                            _channel.Seek(VideoLayer.Preview, MovieSeekOnLoad + newSeek);
                        }
                    }, cancellationTokenSource.Token);
                    _previewPositionCancellationTokenSource = cancellationTokenSource;
                }
            }
        }

        [XmlIgnore, JsonProperty]
        public double AudioVolume
        {
            get => _audioVolume;
            set
            {
                if (SetField(ref _audioVolume, value))
                    _channel?.SetVolume(VideoLayer.Preview, Math.Pow(10, value / 20), 0);
            }
        }

        [XmlIgnore, JsonProperty]
        public bool IsMovieLoaded
        {
            get => _isLoaded;
            private set
            {
                if (!SetField(ref _isLoaded, value))
                    return;
                var vol = _isLoaded ? 0 : _engine.ProgramAudioVolume;
                _channel?.SetVolume(VideoLayer.Program, vol, 0);
            }
        }

        [XmlIgnore, JsonProperty]
        public bool IsPlaying
        {
            get => _isPlaying;
            private set => SetField(ref _isPlaying, value);
        }


        public void LoadMovie(IMedia media, long seek, long duration, long position, double previewAudioVolume)
        {
            if (!_engine.HaveRight(EngineRight.Preview))
                return;
            var mediaToLoad = _findPreviewMedia(media as MediaBase);
            if (mediaToLoad == null)
                return;
            _duration = duration;
            MovieSeekOnLoad = seek;
            _position = position;
            _loadedMovie = media;
            _previewLastPositionSetTick = _currentTicks;
            _channel?.SetAspect(VideoLayer.Preview, media.VideoFormat == TVideoFormat.NTSC
                                                             || media.VideoFormat == TVideoFormat.PAL
                                                             || media.VideoFormat == TVideoFormat.PAL_P);
            IsMovieLoaded = true;
            AudioVolume = previewAudioVolume;
            _channel?.Load(mediaToLoad, VideoLayer.Preview, seek + position, duration - position);
            IsPlaying = false;
            NotifyPropertyChanged(nameof(LoadedMovie));
            NotifyPropertyChanged(nameof(MoviePosition));
            NotifyPropertyChanged(nameof(MovieSeekOnLoad));
        }

        public void LoadStillImage(IMedia media, VideoLayer layer)
        {
            if (!_engine.HaveRight(EngineRight.Preview))
                return;
            var mediaToLoad = _findPreviewMedia(media as MediaBase);
            if (mediaToLoad == null) return;
            if (_channel?.Load(mediaToLoad, layer) != true)
                return;
            _previewLoadedStills.TryAdd(layer, mediaToLoad);
            StillImageLoaded?.Invoke(this, new MediaOnLayerEventArgs(media, layer));
        }

        public bool UnLoadStillImage(VideoLayer layer)
        {
            if (!_previewLoadedStills.TryRemove(layer, out var media))
                return false;
            _channel?.Clear(layer);
            StillImageUnLoaded?.Invoke(this, new MediaOnLayerEventArgs(media, layer));
            return true;
        }

        [JsonProperty]
        public Dictionary<VideoLayer, IMedia> LoadedStillImages => new Dictionary<VideoLayer, IMedia>(_previewLoadedStills);
        
        public void UnloadMovie()
        {
            if (!_engine.HaveRight(EngineRight.Preview))
                return;
            _movieUnload();
        }

        public void Play()
        {
            if (!_engine.HaveRight(EngineRight.Preview))
                return;
            if (_loadedMovie != null && _channel?.Play(VideoLayer.Preview) == true)
                IsPlaying = true;
        }

        public void Pause()
        {
            if (!_engine.HaveRight(EngineRight.Preview))
                return;
            _channel?.Pause(VideoLayer.Preview);
            IsPlaying = false;
        }

        public event EventHandler<MediaOnLayerEventArgs> StillImageLoaded;

        public event EventHandler<MediaOnLayerEventArgs> StillImageUnLoaded;

        private void _movieUnload()
        {
            var channel = _channel;
            var media = _loadedMovie;
            if (channel == null || media == null)
                return;
            _previewPositionCancellationTokenSource?.Cancel();
            channel.Clear(VideoLayer.Preview);
            _duration = 0;
            _position = 0;
            MovieSeekOnLoad = 0;
            _loadedMovie = null;
            IsMovieLoaded = false;
            IsPlaying = false;
            NotifyPropertyChanged(nameof(LoadedMovie));
            NotifyPropertyChanged(nameof(MoviePosition));
            NotifyPropertyChanged(nameof(MovieSeekOnLoad));
        }

        internal void Tick(long currentTicks, long nFrames)
        {
            _currentTicks = currentTicks;
            if (!IsPlaying)
                return;
            if (_position < _duration - 1)
            {
                _position += nFrames;
                NotifyPropertyChanged(nameof(MoviePosition));
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
           _channel.PropertyChanged -= ChannelPropertyChanged;
        }

        private MediaBase _findPreviewMedia(MediaBase media)
        {
            var playoutChannel = _channel;
            if (!(media is ServerMedia))
                return media;
            if (playoutChannel == null)
                return null;
            return media.Directory == playoutChannel.Owner.MediaDirectory
                ? media
                : ((ServerDirectory)playoutChannel.Owner.MediaDirectory).FindMediaByMediaGuid(media.MediaGuid);
        }

        private void ChannelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPlayoutServerChannel.IsServerConnected))
                NotifyPropertyChanged(nameof(IsConnected));
        }

    }
}
