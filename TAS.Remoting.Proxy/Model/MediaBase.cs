using Newtonsoft.Json;
using System;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public abstract class MediaBase : ProxyBase, IMedia
    {
        #pragma warning disable CS0649 

        [JsonProperty(nameof(IMedia.AudioChannelMapping))]
        private TAudioChannelMapping _audioChannelMapping;

        [JsonProperty(nameof(IMedia.AudioLevelIntegrated))]
        private decimal _audioLevelIntegrated;

        [JsonProperty(nameof(IMedia.AudioLevelPeak))]
        private decimal _audioLevelPeak;

        [JsonProperty(nameof(IMedia.AudioVolume))]
        private decimal _audioVolume;

        [JsonProperty(nameof(IMedia.Duration))]
        private TimeSpan _duration;

        [JsonProperty(nameof(IMedia.DurationPlay))]
        private TimeSpan _durationPlay;

        [JsonProperty(nameof(IMedia.FileName))]
        private string _fileName;

        [JsonProperty(nameof(IMedia.FileSize))]
        private ulong _fileSize;

        [JsonProperty(nameof(IMedia.Folder))]
        private string _folder;

        [JsonProperty(nameof(IMedia.LastUpdated))]
        private DateTime _lastUpdated;

        [JsonProperty(nameof(IMedia.MediaCategory))]
        private TMediaCategory _mediaCategory;

        [JsonProperty(nameof(IMedia.MediaGuid))]
        private Guid _mediaGuid;

        [JsonProperty(nameof(IMedia.MediaName))]
        private string _mediaName;

        [JsonProperty(nameof(IMedia.MediaStatus))]
        private TMediaStatus _mediaStatus;

        [JsonProperty(nameof(IMedia.MediaType))]
        private TMediaType _mediaType;

        [JsonProperty(nameof(IMedia.Parental))]
        private byte _parental;

        [JsonProperty(nameof(IMedia.TcPlay))]
        private TimeSpan _tcPlay;

        [JsonProperty(nameof(IMedia.TcStart))]
        private TimeSpan _tcStart;

        [JsonProperty(nameof(IMedia.IsVerified))]
        private bool _isVerified;

        [JsonProperty(nameof(IMedia.VideoFormat))]
        private TVideoFormat _videoFormat;

        [JsonProperty(nameof(IMedia.FieldOrderInverted))]
        private bool _fieldOrderInverted;

        [JsonProperty(nameof(IMedia.Directory))]
        private MediaDirectory _directory;

        #pragma warning restore

        public TAudioChannelMapping AudioChannelMapping { get { return _audioChannelMapping; } set { Set(value); } }

        public decimal AudioLevelIntegrated { get { return _audioLevelIntegrated; } set { Set(value); } }

        public decimal AudioLevelPeak { get { return _audioLevelPeak; } set { Set(value); } }

        public decimal AudioVolume { get { return _audioVolume; } set { Set(value); } }

        public virtual IMediaDirectory Directory => _directory;

        public TimeSpan Duration { get { return _duration; } set { Set(value); } }

        public TimeSpan DurationPlay { get { return _durationPlay; } set { Set(value); } }

        public string FileName { get { return _fileName; } set { Set(value); } }

        public ulong FileSize { get { return _fileSize; } set { Set(value); } }

        public string Folder { get { return _folder; } set { Set(value); } }

        public DateTime LastUpdated { get { return _lastUpdated; } set { Set(value); } }

        public TMediaCategory MediaCategory { get { return _mediaCategory; } set { Set(value); } }

        public Guid MediaGuid { get { return _mediaGuid; } set { Set(value); } }

        public string MediaName { get { return _mediaName; } set { Set(value); } }

        public TMediaStatus MediaStatus { get { return _mediaStatus; } set { Set(value); } }

        public TMediaType MediaType { get { return _mediaType; } set { Set(value); } }

        public byte Parental { get { return _parental; } set { Set(value); } }

        public TimeSpan TcPlay { get { return _tcPlay; } set { Set(value); } }

        public TimeSpan TcStart { get { return _tcStart; } set { Set(value); } }

        public bool IsVerified => _isVerified;
 
        public TVideoFormat VideoFormat { get { return _videoFormat; } set { Set(value); } }

        public bool FieldOrderInverted { get { return _fieldOrderInverted; } set { Set(value); } }

        public bool Delete()
        {
            return Query<bool>();
        }

        public bool FileExists()
        {
            return Query<bool>();
        }

        public void GetLoudness()
        {
            Invoke();
        }

        public void ReVerify()
        {
            Invoke();
        }

        public void Verify()
        {
            Invoke();
        }

        public override string ToString()
        {
            return $"{MediaName}";
        }

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
