//#undef DEBUG

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using TAS.Remoting.Server;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    [DebuggerDisplay("{_directory.DirectoryName}:{_mediaName} ({FullPath})")]
    public abstract class MediaBase : DtoBase, IMedia
    {
        private string _folder = string.Empty;
        private string _fileName = string.Empty;
        private ulong _fileSize;
        private DateTime _lastUpdated;
        private string _mediaName;
        private TMediaType _mediaType;
        private TimeSpan _duration;
        private TimeSpan _durationPlay;
        private TimeSpan _tcStart;
        private TimeSpan _tcPlay;
        private TVideoFormat _videoFormat;
        private bool _fieldOrderInverted;
        private TAudioChannelMapping _audioChannelMapping;
        private double _audioVolume;
        private double _audioLevelIntegrated;
        private double _audioLevelPeak;
        private TMediaCategory _mediaCategory;
        private byte _parental;
        private Guid _mediaGuid;
        private bool _verified;
        private TMediaStatus _mediaStatus;
        private readonly MediaDirectory _directory;
        internal bool HasExtraLines; // VBI lines that shouldn't be displayed


        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(MediaBase));

        protected MediaBase(IMediaDirectory directory, Guid mediaGuid = default(Guid))
        {
            _directory = (MediaDirectory)directory;
            _mediaGuid = mediaGuid == default(Guid)? Guid.NewGuid() : mediaGuid;
            _directory.MediaAdd(this);
        }

        #region IMediaProperties
        [JsonProperty]
        public string Folder
        {
            get => _folder;
            set
            {
                if (SetField(ref _folder, value))
                    NotifyPropertyChanged(nameof(FullPath));
            }
        }

        [JsonProperty]
        public string FileName
        {
            get => _fileName;
            set
            {
                var oldFullPath = FullPath;
                if (_fileName != value
                    && MediaStatus == TMediaStatus.Available
                    && File.Exists(oldFullPath))
                {
                    try
                    {
                        File.Move(oldFullPath, _getFullPath(value));
                        _fileName = value;
                    }
                    catch (Exception e)
                    {
                        Logger.Warn(e, "File {0} rename failed", this);
                    }
                    NotifyPropertyChanged(nameof(FileName));
                }
                else
                if (SetField(ref _fileName, value))
                    NotifyPropertyChanged(nameof(FullPath));
            }
        }

        [JsonProperty]
        public ulong FileSize 
        {
            get => _fileSize;
            set => SetField(ref _fileSize, value);
        }

        [JsonProperty]
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetField(ref _lastUpdated, value);
        }
        //// to enable LastAccess: "FSUTIL behavior set disablelastaccess 0" on NTFS volume
        //// not stored in datebase
        //protected DateTime _lastAccess;
        //public DateTime LastAccess
        //{
        //    get { return _lastAccess; }
        //    internal set
        //    {
        //        if (_lastAccess != value)
        //        {
        //            _lastAccess = value;
        //            NotifyPropertyChanged("LastAccess");
        //        }
        //    }
        //}

        // media parameters
        [JsonProperty]
        public virtual string MediaName
        {
            get => _mediaName;
            set => SetField(ref _mediaName, value);
        }

        [JsonProperty]
        public TMediaType MediaType
        {
            get => _mediaType;
            set => SetField(ref _mediaType, value);
        }

        [JsonProperty]
        public virtual TimeSpan Duration
        {
            get => _duration;
            set => SetField(ref _duration, value);
        }

        [JsonProperty]
        public virtual TimeSpan DurationPlay
        {
            get => _durationPlay;
            set => SetField(ref _durationPlay, value);
        }

        [JsonProperty]
        public virtual TimeSpan TcStart 
        {
            get => _tcStart;
            set => SetField(ref _tcStart, value);
        }

        [JsonProperty]
        public virtual TimeSpan TcPlay
        {
            get { return _tcPlay; }
            set { SetField(ref _tcPlay, value); }
        }

        [JsonProperty]
        public virtual TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            set { SetField(ref _videoFormat, value); }
        }

        [JsonProperty]
        public virtual bool FieldOrderInverted
        {
            get { return _fieldOrderInverted; }
            set { SetField(ref _fieldOrderInverted, value); }
        }

        [JsonProperty]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public virtual TAudioChannelMapping AudioChannelMapping 
        {
            get { return _audioChannelMapping; }
            set { SetField(ref _audioChannelMapping, value); }
        }

        [JsonProperty]
        public virtual double AudioVolume // correction amount on play
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value); }
        }

        [JsonProperty]
        public virtual double AudioLevelIntegrated //measured
        {
            get { return _audioLevelIntegrated; }
            set { SetField(ref _audioLevelIntegrated, value); }
        }

        [JsonProperty]
        public virtual double AudioLevelPeak //measured
        {
            get { return _audioLevelPeak; }
            set { SetField(ref _audioLevelPeak, value); }
        }

        [JsonProperty]
        public virtual TMediaCategory MediaCategory
        {
            get { return _mediaCategory; }
            set { SetField(ref _mediaCategory, value); }
        }

        [JsonProperty]
        public virtual byte Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value); }
        }
        
        [JsonProperty]
        public Guid MediaGuid
        {
            get { return _mediaGuid; }
            internal set
            {
                Guid oldGuid = _mediaGuid;
                if (SetField(ref _mediaGuid, value))
                    _directory.UpdateMediaGuid(oldGuid, this);
            }
        }

        [JsonProperty]
        public TMediaStatus MediaStatus
        {
            get { return _mediaStatus; }
            set { SetField(ref _mediaStatus, value); }
        }

        [JsonProperty]
        public bool IsVerified
        {
            get { return _verified; }
            internal set
            {
                if (SetField(ref _verified, value) && value && _mediaStatus == TMediaStatus.Available)
                    _directory.NotifyMediaVerified(this);
            }
        }

        [JsonProperty]
        public IMediaDirectory Directory => _directory;

        #endregion //IMediaProperties

        public string FullPath
        {
            get { return _getFullPath(_fileName); }
            internal set
            {
                string relativeName = value.Substring(_directory.Folder.Length);
                FileName = Path.GetFileName(relativeName);
                Folder = relativeName.Substring(0, relativeName.Length - _fileName.Length).Trim(_directory.PathSeparator);
            }
        }

        public virtual bool Delete()
        {
            return ((MediaDirectory)Directory).DeleteMedia(this);
        }

        public virtual void CloneMediaProperties(IMediaProperties fromMedia)
        {
            MediaName = fromMedia.MediaName;
            AudioChannelMapping = fromMedia.AudioChannelMapping;
            AudioVolume = fromMedia.AudioVolume;
            AudioLevelIntegrated = fromMedia.AudioLevelIntegrated;
            AudioLevelPeak = fromMedia.AudioLevelPeak;
            Duration = fromMedia.Duration;
            DurationPlay = fromMedia.DurationPlay;
            TcStart = fromMedia.TcStart;
            TcPlay = fromMedia.TcPlay;
            MediaType = fromMedia.MediaType;
            VideoFormat = fromMedia.VideoFormat;
            MediaCategory = fromMedia.MediaCategory;
            Parental = fromMedia.Parental;
        }

        public virtual Stream GetFileStream(bool forWrite)
        {
            return new FileStream(FullPath, forWrite ? FileMode.Create : FileMode.Open);
        }

        public virtual bool CopyMediaTo(MediaBase destMedia, ref bool abortCopy)
        {
            if ((!(_directory is IngestDirectory sIngestDir) || sIngestDir.AccessType == TDirectoryAccessType.Direct)
                && (!(destMedia._directory is IngestDirectory dIngestDir) || dIngestDir.AccessType == TDirectoryAccessType.Direct))
            {
                FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(destMedia.FullPath));
                File.Copy(FullPath, destMedia.FullPath, true);
                File.SetCreationTimeUtc(destMedia.FullPath, File.GetCreationTimeUtc(FullPath));
                File.SetLastWriteTimeUtc(destMedia.FullPath, File.GetLastWriteTimeUtc(FullPath));
            }
            else
            {
                using (Stream source = GetFileStream(false),
                                dest = destMedia.GetFileStream(true))
                {
                    if (source == null || dest == null)
                        return false;
                    var buffer = new byte[1024 * 1024];
                    ulong totalReadBytesCount = 0;
                    int readBytesCount;
                    FileSize = (ulong)source.Length;
                    while ((readBytesCount = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (abortCopy)
                            return false;
                        dest.Write(buffer, 0, readBytesCount);
                        totalReadBytesCount += (ulong)readBytesCount;
                        destMedia.FileSize = totalReadBytesCount;
                    }
                }
            }
            return true;
        }
        
        public override string ToString()
        {
            return $"{_directory?.DirectoryName}:{MediaName}";
        }

        public virtual bool FileExists()
        {
            return File.Exists(FullPath);
        }

        public void Remove()
        {
            ((MediaDirectory)Directory).MediaRemove(this);
        }


        public void ReVerify()
        {
            MediaStatus = TMediaStatus.Unknown;
            IsVerified = false;
            ThreadPool.QueueUserWorkItem((o) => Verify());
        }

        public virtual void Verify()
        {
            if (IsVerified || (_mediaStatus == TMediaStatus.Copying) || (_mediaStatus == TMediaStatus.CopyPending || _mediaStatus == TMediaStatus.Required))
                return;
            if (_directory != null && System.IO.Directory.Exists(_directory.Folder) && !File.Exists(FullPath))
            {
                _mediaStatus = TMediaStatus.Deleted;
                return; // in case that no file was found, and directory exists
            }
            try
            {
                FileInfo fi = new FileInfo(FullPath);
                if (fi.Length == 0L)
                    return;
                if ((MediaType != TMediaType.Animation)
                    &&
                    (MediaStatus == TMediaStatus.Unknown
                    || MediaStatus == TMediaStatus.Deleted
                    || MediaStatus == TMediaStatus.Copied
                    || (MediaType != TMediaType.Still && Duration == TimeSpan.Zero)
                    || FileSize != (UInt64)fi.Length
                    || !LastUpdated.DateTimeEqualToDays(fi.LastWriteTimeUtc)
                    ))
                {
                    FileSize = (ulong)fi.Length;
                    LastUpdated = DateTimeExtensions.FromFileTime(fi.LastWriteTimeUtc, DateTimeKind.Utc);
                    //this.LastAccess = DateTimeExtensions.FromFileTime(fi.LastAccessTimeUtc, DateTimeKind.Utc);

                    this.Check();
                }                
                IsVerified = true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Verify {0} exception", this);
                Debug.WriteLine(e);
            }
        }

        public void GetLoudness()
        {
            _directory.MediaManager.FileManager.Queue(
                new LoudnessOperation((FileManager) _directory.MediaManager.FileManager)
                {
                    Source = this,
                    MeasureStart = TcPlay - TcStart,
                    MeasureDuration = DurationPlay
                }, false);
        }

        protected override void DoDispose()
        {
            Debug.WriteLine(this, "Disposed");
            base.DoDispose();
        }


        private string _getFullPath(string fileName)
        {
            return string.IsNullOrWhiteSpace(_folder) ?
                string.Join(_directory.PathSeparator.ToString(), _directory.Folder.TrimEnd(_directory.PathSeparator), fileName) :
                string.Join(_directory.PathSeparator.ToString(), _directory.Folder.TrimEnd(_directory.PathSeparator), _folder, fileName);
        }

    }

}
