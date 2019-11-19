using System;
using ComponentModelRPC;
using ComponentModelRPC.Client;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    public class Preview: ProxyBase, IPreview
    {
#pragma warning disable CS0649
        [JsonProperty(nameof(IPreview.Channel))]
        private PlayoutServerChannel _channel;

        [JsonProperty(nameof(IPreview.AudioVolume))]
        private double _audioVolume;

        [JsonProperty(nameof(IPreview.IsConnected))]
        private bool _isConnected;

        [JsonProperty(nameof(IPreview.IsPlaying))]
        private bool _isPlaying;

        [JsonProperty(nameof(IPreview.IsLoaded))]
        private bool _loaded;

        [JsonProperty(nameof(IPreview.Media))]
        private MediaBase _media;

        [JsonProperty(nameof(IPreview.Position))]
        private long _position;

        [JsonProperty(nameof(IPreview.LoadedSeek))]
        private long _seek;

        [JsonProperty(nameof(IPreview.FormatDescription))]
        private VideoFormatDescription _formatDescription;

#pragma warning restore

        public IPlayoutServerChannel Channel => _channel;

        public double AudioVolume
        {
            get => _audioVolume;
            set => Set(value);
        }

        public bool IsConnected => _isConnected;

        public bool IsPlaying => _isPlaying;

        public bool IsLoaded => _loaded;

        public void LoadStill(IMedia media, VideoLayer layer)
        {
            Invoke(parameters: new object[] {media, layer});
        }

        public IMedia Media => _media;

        public long Position { get => _position; set => Set(value); }

        public long LoadedSeek => _seek;

        public VideoFormatDescription FormatDescription => _formatDescription;

        public void LoadMovie(IMedia media, long seek, long duration, long position, double audioLevel)
        {
            Invoke(parameters: new object[] { media, seek, duration, position, audioLevel });
        }

        public void Pause() { Invoke(); }

        public void Play() { Invoke(); }

        public void Unload() { Invoke(); }

        private event EventHandler<MediaOnLayerEventArgs> _previewStillLoaded;
        public event EventHandler<MediaOnLayerEventArgs> StillLoaded
        {
            add
            {
                EventAdd(_previewStillLoaded);
                _previewStillLoaded += value;
            }
            remove
            {
                _previewStillLoaded -= value;
                EventRemove(_loaded);
            }
        }

        protected override void OnEventNotification(SocketMessage message)
        {
            switch (message.MemberName)
            {
                case nameof(IPreview.StillLoaded):
                    _previewStillLoaded?.Invoke(this, Deserialize<MediaOnLayerEventArgs>(message));
                    break;
            }
        }
    }
}
