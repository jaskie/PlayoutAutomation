using System;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public abstract class MediaBase : ProxyObjectBase, IMedia
    {
        #pragma warning disable CS0649 

        [DtoMember(nameof(IMedia.AudioChannelMapping))]
        private TAudioChannelMapping _audioChannelMapping;

        [DtoMember(nameof(IMedia.AudioLevelIntegrated))]
        private double _audioLevelIntegrated;

        [DtoMember(nameof(IMedia.AudioLevelPeak))]
        private double _audioLevelPeak;

        [DtoMember(nameof(IMedia.AudioVolume))]
        private double _audioVolume;

        [DtoMember(nameof(IMedia.Directory))]
        private MediaDirectoryBase _directory;

        [DtoMember(nameof(IMedia.Duration))]
        private TimeSpan _duration;

        [DtoMember(nameof(IMedia.DurationPlay))]
        private TimeSpan _durationPlay;

        [DtoMember(nameof(IMedia.FileName))]
        private string _fileName;

        [DtoMember(nameof(IMedia.FileSize))]
        private ulong _fileSize;

        [DtoMember(nameof(IMedia.Folder))]
        private string _folder;

        [DtoMember(nameof(IMedia.LastUpdated))]
        private DateTime _lastUpdated;

        [DtoMember(nameof(IMedia.MediaCategory))]
        private TMediaCategory _mediaCategory;

        [DtoMember(nameof(IMedia.MediaGuid))]
        private Guid _mediaGuid;

        [DtoMember(nameof(IMedia.MediaName))]
        private string _mediaName;

        [DtoMember(nameof(IMedia.MediaStatus))]
        private TMediaStatus _mediaStatus;

        [DtoMember(nameof(IMedia.MediaType))]
        private TMediaType _mediaType;

        [DtoMember(nameof(IMedia.Parental))]
        private byte _parental;

        [DtoMember(nameof(IMedia.TcPlay))]
        private TimeSpan _tcPlay;

        [DtoMember(nameof(IMedia.TcStart))]
        private TimeSpan _tcStart;

        [DtoMember(nameof(IMedia.IsVerified))]
        private bool _isVerified;

        [DtoMember(nameof(IMedia.VideoFormat))]
        private TVideoFormat _videoFormat;

        [DtoMember(nameof(IMedia.FieldOrderInverted))]
        private bool _fieldOrderInverted;

        #pragma warning restore

        public TAudioChannelMapping AudioChannelMapping
        {
            get => _audioChannelMapping;
            set => Set(value);
        }

        public double AudioLevelIntegrated
        {
            get => _audioLevelIntegrated;
            set => Set(value);
        }

        public double AudioLevelPeak
        {
            get => _audioLevelPeak;
            set => Set(value);
        }

        public double AudioVolume
        {
            get => _audioVolume;
            set => Set(value);
        }

        public virtual IMediaDirectory Directory => _directory;

        public TimeSpan Duration
        {
            get => _duration;
            set => Set(value);
        }

        public TimeSpan DurationPlay
        {
            get => _durationPlay;
            set => Set(value);
        }

        public string FileName
        {
            get => _fileName;
            set => Set(value);
        }

        public ulong FileSize
        {
            get => _fileSize;
            set => Set(value);
        }

        public string Folder
        {
            get => _folder;
            set => Set(value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => Set(value);
        }

        public TMediaCategory MediaCategory
        {
            get => _mediaCategory;
            set => Set(value);
        }

        public Guid MediaGuid
        {
            get => _mediaGuid;
            set => Set(value);
        }

        public string MediaName
        {
            get => _mediaName;
            set => Set(value);
        }

        public TMediaStatus MediaStatus
        {
            get => _mediaStatus;
            set => Set(value);
        }

        public TMediaType MediaType
        {
            get => _mediaType;
            set => Set(value);
        }

        public byte Parental
        {
            get => _parental;
            set => Set(value);
        }

        public TimeSpan TcPlay
        {
            get => _tcPlay;
            set => Set(value);
        }

        public TimeSpan TcStart
        {
            get => _tcStart;
            set => Set(value);
        }

        public bool IsVerified => _isVerified;

        public TVideoFormat VideoFormat
        {
            get => _videoFormat;
            set => Set(value);
        }

        public bool FieldOrderInverted
        {
            get => _fieldOrderInverted;
            set => Set(value);
        }

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

        public void RenameFileTo(string newFileName)
        {
            Invoke(parameters: new object[] {newFileName});
        }

        public void Verify(bool updateFormatAndDurations)
        {
            Invoke(parameters: new object[] {updateFormatAndDurations});
        }

        public override string ToString()
        {
            return $"Media: {MediaName}";
        }

        protected override void OnEventNotification(SocketMessage message) { }

    }
}
