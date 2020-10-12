#undef DEBUG

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.MediaOperation;

namespace TAS.Server.Media
{
    [DebuggerDisplay("{Directory}:{_mediaName} ({FullPath})")]
    public abstract class MediaBase : ServerObjectBase, IMedia
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
        internal bool HasExtraLines; // VBI lines that shouldn't be displayed
        private bool _hasTransparency;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #region IMediaProperties
        [DtoMember]
        public string Folder
        {
            get => _folder;
            set
            {
                if (SetField(ref _folder, value))
                    NotifyPropertyChanged(nameof(FullPath));
            }
        }

        [DtoMember]
        public string FileName
        {
            get => _fileName;
            set
            {
                if (SetField(ref _fileName, value))
                    NotifyPropertyChanged(nameof(FullPath));
            }
        }

        [DtoMember]
        public ulong FileSize
        {
            get => _fileSize;
            set => SetField(ref _fileSize, value);
        }

        [DtoMember]
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
        [DtoMember]
        public virtual string MediaName
        {
            get => _mediaName;
            set => SetField(ref _mediaName, value);
        }

        [DtoMember]
        public TMediaType MediaType
        {
            get => _mediaType;
            set => SetField(ref _mediaType, value);
        }

        [DtoMember]
        public virtual TimeSpan Duration
        {
            get => _duration;
            set => SetField(ref _duration, value);
        }

        [DtoMember]
        public virtual TimeSpan DurationPlay
        {
            get => _durationPlay;
            set => SetField(ref _durationPlay, value);
        }

        [DtoMember]
        public virtual TimeSpan TcStart
        {
            get => _tcStart;
            set => SetField(ref _tcStart, value);
        }

        [DtoMember]
        public virtual TimeSpan TcPlay
        {
            get => _tcPlay;
            set => SetField(ref _tcPlay, value);
        }

        [DtoMember]
        public virtual TVideoFormat VideoFormat
        {
            get => _videoFormat;
            set => SetField(ref _videoFormat, value);
        }

        [DtoMember]
        public virtual bool FieldOrderInverted
        {
            get => _fieldOrderInverted;
            set => SetField(ref _fieldOrderInverted, value);
        }

        [DtoMember]
        public virtual TAudioChannelMapping AudioChannelMapping
        {
            get => _audioChannelMapping;
            set => SetField(ref _audioChannelMapping, value);
        }

        [DtoMember]
        public virtual double AudioVolume // correction amount on play
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        [DtoMember]
        public virtual double AudioLevelIntegrated //measured
        {
            get => _audioLevelIntegrated;
            set => SetField(ref _audioLevelIntegrated, value);
        }

        [DtoMember]
        public virtual double AudioLevelPeak //measured
        {
            get => _audioLevelPeak;
            set => SetField(ref _audioLevelPeak, value);
        }

        [DtoMember]
        public virtual TMediaCategory MediaCategory
        {
            get => _mediaCategory;
            set => SetField(ref _mediaCategory, value);
        }

        [DtoMember]
        public virtual byte Parental
        {
            get => _parental;
            set => SetField(ref _parental, value);
        }

        [DtoMember]
        public Guid MediaGuid
        {
            get => _mediaGuid;
            set => SetField(ref _mediaGuid, value);
        }

        [DtoMember]
        public TMediaStatus MediaStatus
        {
            get => _mediaStatus;
            set => SetField(ref _mediaStatus, value);
        }

        [DtoMember]
        public bool IsVerified
        {
            get => _verified;
            internal set
            {
                if (SetField(ref _verified, value) && value && _mediaStatus == TMediaStatus.Available)
                    (Directory as MediaDirectoryBase)?.NotifyMediaVerified(this);
            }
        }

        [DtoMember]
        public bool HasTransparency { get => _hasTransparency; set => SetField(ref _hasTransparency, value); }

        [DtoMember]
        public IMediaDirectory Directory { get; internal set; }

        #endregion //IMediaProperties

        public string FullPath => _getFullPath(_fileName);

        public virtual bool Delete()
        {
            if (!((MediaDirectoryBase)Directory).DeleteMedia(this))
                return false;
            MediaStatus = TMediaStatus.Deleted;
            return true;
        }

        internal virtual void CloneMediaProperties(IMediaProperties fromMedia)
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
            HasTransparency = fromMedia.HasTransparency;
        }

        public virtual Stream GetFileStream(bool forWrite)
        {
            return new FileStream(FullPath, forWrite ? FileMode.Create : FileMode.Open);
        }

        public virtual async Task<bool> CopyMediaTo(MediaBase destMedia, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                if ((!(Directory is IngestDirectory sIngestDir) || sIngestDir.AccessType == TDirectoryAccessType.Direct)
                    && (!(destMedia.Directory is IngestDirectory dIngestDir) ||
                        dIngestDir.AccessType == TDirectoryAccessType.Direct))
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
                            if (cancellationToken.IsCancellationRequested)
                                return false;
                            dest.Write(buffer, 0, readBytesCount);
                            totalReadBytesCount += (ulong)readBytesCount;
                            destMedia.FileSize = totalReadBytesCount;
                        }
                    }
                }
                return true;
            }, cancellationToken);
        }

        public void RenameFileTo(string newFileName)
        {
            if (_fileName == newFileName || MediaStatus != TMediaStatus.Available || !File.Exists(FullPath))
                return;
            try
            {
                File.Move(FullPath, _getFullPath(newFileName));
                FileName = newFileName;
            }
            catch (Exception e)
            {
                Logger.Warn(e, "File {0} rename failed", this);
            }
        }

        public override string ToString()
        {
            return $"{Directory}:{Folder}\\{MediaName}";
        }

        public virtual bool FileExists()
        {
            return File.Exists(FullPath);
        }

        public void Remove()
        {
            ((WatcherDirectory)Directory).RemoveMedia(this);
        }

        public virtual void Verify(bool updateFormatAndDurations)
        {
            if (_mediaStatus == TMediaStatus.Copying || _mediaStatus == TMediaStatus.CopyPending || _mediaStatus == TMediaStatus.Required ||
                (Directory is IngestDirectory ingestDirectory && ingestDirectory.AccessType != TDirectoryAccessType.Direct))
                return;
            if (Directory?.DirectoryExists == true && !FileExists())
            {
                _mediaStatus = TMediaStatus.Deleted;
                return; // in case that no file was found, and directory exists
            }
            try
            {
                var fi = new FileInfo(FullPath);
                if (fi.Length == 0L)
                    return;
                if ((MediaType != TMediaType.Animation)
                    &&
                    (MediaStatus == TMediaStatus.Unknown
                    || MediaStatus == TMediaStatus.Deleted
                    || MediaStatus == TMediaStatus.Copied
                    || FileSize != (ulong)fi.Length
                    || !LastUpdated.DateTimeEqualToDays(fi.LastWriteTimeUtc)
                    ))
                {
                    FileSize = (ulong)fi.Length;
                    LastUpdated = fi.LastWriteTimeUtc.FromFileTime(DateTimeKind.Utc);
                    //this.LastAccess = DateTimeExtensions.FromFileTime(fi.LastAccessTimeUtc, DateTimeKind.Utc);

                    this.Check(updateFormatAndDurations);
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
            FileManager.Current.Queue(
                new LoudnessOperation
                {
                    Source = this,
                    MeasureStart = TcPlay - TcStart,
                    MeasureDuration = DurationPlay
                });
        }

        protected override void DoDispose()
        {
            Debug.WriteLine(this, "Disposed");
            base.DoDispose();
        }


        private string _getFullPath(string fileName)
        {
            return string.IsNullOrWhiteSpace(_folder) ?
                string.Join(Directory.PathSeparator.ToString(), Directory.Folder.TrimEnd(Directory.PathSeparator), fileName) :
                string.Join(Directory.PathSeparator.ToString(), Directory.Folder.TrimEnd(Directory.PathSeparator), _folder, fileName);
        }

    }

}
