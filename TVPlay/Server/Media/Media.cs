//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using TAS.Common;
using System.Net.FtpClient;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using TAS.FFMpegUtils;

namespace TAS.Server
{

    public abstract class Media : INotifyPropertyChanged
    {

#if DEBUG
        ~Media()
        {
            Debug.WriteLine(this, "Finalized");
        }
#endif // DEBUG

        // file properties
        internal string _folder;
        protected virtual string GetFolder()
        {
            return _folder == null ? string.Empty : _folder;
        }
        public string Folder
        {
            get { return GetFolder(); }
            internal set { SetField(ref _folder, value, "Folder"); }
        }
        internal string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set 
            {
                string prevFileName = _fileName;
                if (value != prevFileName)
                {
                    if (_mediaStatus == TMediaStatus.Available)
                        RenameTo(value);
                    else
                        SetField(ref _fileName, value, "FileName");
                }
            }
        }
        internal void Renamed(string newName)
        {
            SetField(ref _fileName, newName, "FileName");
        }

        internal UInt64 _fileSize;
        public UInt64 FileSize 
        {
            get { return _fileSize; }
            internal set { SetField(ref _fileSize, value, "FileSize"); }
        }

        internal DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            internal set { SetField(ref _lastUpdated, value, "LastUpdated"); }
        }
        //// to enable LastAccess: "FSUTIL behavior set disablelastaccess 0" on NTFS volume
        //// not stored in datebase
        //private DateTime _lastAccess;
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
        internal string _mediaName;
        public virtual string MediaName
        {
            get { return _mediaName; }
            set { SetField(ref _mediaName, value, "MediaName"); }
        }

        internal TMediaType _mediaType;
        public virtual TMediaType MediaType
        {
            get { return _mediaType; }
            set { SetField(ref _mediaType, value, "MediaType"); }
        }

        internal TimeSpan _duration;
        public virtual TimeSpan Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value, "Duration"); }
        }
        internal TimeSpan _durationPlay;
        public virtual TimeSpan DurationPlay
        {
            get { return _durationPlay; }
            set { SetField(ref _durationPlay, value, "DurationPlay"); }
        }
        internal TimeSpan _tCStart;
        public virtual TimeSpan TCStart 
        {
            get { return _tCStart; }
            set { SetField(ref _tCStart, value, "TCStart"); }
        }
        internal TimeSpan _tCPlay;
        public virtual TimeSpan TCPlay
        {
            get { return _tCPlay; }
            set { SetField(ref _tCPlay, value, "TCPlay"); }
        }
        internal TVideoFormat _videoFormat;
        public virtual TVideoFormat VideoFormat 
        {
            get { return _videoFormat; }
            set { SetField(ref _videoFormat, value, "VideoFormat"); }
        }
        internal TAudioChannelMapping _audioChannelMapping;
        public virtual TAudioChannelMapping AudioChannelMapping 
        {
            get { return _audioChannelMapping; }
            set { SetField(ref _audioChannelMapping, value, "AudioChannelMapping"); }
        }
        internal decimal _audioVolume;
        public virtual decimal AudioVolume // correction amount on play
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, "AudioVolume"); }
        }

        internal decimal _audioLevelIntegrated;
        public virtual decimal AudioLevelIntegrated //measured
        {
            get { return _audioLevelIntegrated; }
            set { SetField(ref _audioLevelIntegrated, value, "AudioLevelIntegrated"); }
        }

        internal decimal _audioLevelPeak;
        public virtual decimal AudioLevelPeak //measured
        {
            get { return _audioLevelPeak; }
            set { SetField(ref _audioLevelPeak, value, "AudioLevelPeak"); }
        }

        internal TMediaCategory _mediaCategory;
        public virtual TMediaCategory MediaCategory
        {
            get { return _mediaCategory; }
            set { SetField(ref _mediaCategory, value, "MediaCategory"); }
        }

        internal TParental _parental;
        public virtual TParental Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value, "Parental"); }
        }

        internal Guid _mediaGuid;
        public virtual Guid MediaGuid
        {
            get { return _mediaGuid; }
            internal set { SetField(ref _mediaGuid, value, "MediaGuid"); }
        }

        public bool HasExtraLines { get; internal set; }

        private VideoFormatDescription _videoFormatDescription;
        public VideoFormatDescription VideoFormatDescription
        {
            get
            {
                var vfd = _videoFormatDescription;
                return vfd != null ? vfd : VideoFormatDescription.Descriptions[VideoFormat];
            }
            internal set { _videoFormatDescription = value; }
        }
        
        protected MediaDirectory _directory;
        public MediaDirectory Directory
        {
            get { return _directory; }
            internal set 
            {
                if (value != _directory)
                {
                    if (_directory != null)
                        _directory.MediaRemove(this);
                    _directory = value;
                    if (_directory != null)
                        _directory.MediaAdd(this);
                }
            }
        }

        public string FullPath
        {
            get
            {
                var dir = _directory;
                var df = dir == null ? string.Empty : dir.Folder;
                if (df == null)
                    df = string.Empty;
                var folder = _folder;
                if (folder == null)
                    folder = string.Empty;
                var fn = _fileName;
                if (fn == null)
                    fn = string.Empty;
                string fullPath = Path.Combine(df, folder, fn);
                if (dir != null && dir.AccessType == TDirectoryAccessType.FTP)
                {
                    return fullPath.Replace('\\', '/');
                }
                return fullPath;
            }
            set 
            {
                FileName = Path.GetFileName(value);
                if (value.StartsWith(_directory.Folder))
                    Folder = Path.GetDirectoryName(value).Substring(_directory.Folder.Length);
                else
                    Folder = "";
            }
        }

        internal virtual bool Delete()
        {
            MediaDirectory d = this.Directory;
            if (d != null)
                return Directory.DeleteMedia(this);
            else
                throw new ApplicationException(string.Format("Cannot delete {1}. Directory unknown.", this));
        }

        protected virtual bool RenameTo(string NewFileName)
        {
            try
            {
                MediaDirectory d = this.Directory;
                if (d != null)
                {
                    if (d.AccessType == TDirectoryAccessType.Direct)
                    {
                        File.Move(FullPath, Path.Combine(d.Folder, this.Folder, NewFileName));
                        return true;
                    }
                    else throw new NotImplementedException("Cannot rename on remote directories");
                }
                else throw new ApplicationException(string.Format("Cannot rename {1}. Directory unknown.", this));
            }
            catch { }
            return false;
        }

        //protected virtual void setMediaStatus(TMediaStatus newStatus)
        //{
        //    SetField(ref _mediaStatus, newStatus, "MediaStatus");
        //}

        internal TMediaStatus _mediaStatus;
        public TMediaStatus MediaStatus
        {
            get { return _mediaStatus; }
            internal set { SetField(ref _mediaStatus, value, "MediaStatus"); }
                //setMediaStatus(value); }
        }

        public virtual void CloneMediaProperties(Media fromMedia)
        {
            MediaGuid = fromMedia.MediaGuid;
            MediaName = fromMedia.MediaName;
            AudioChannelMapping = fromMedia.AudioChannelMapping;
            AudioVolume = fromMedia.AudioVolume;
            AudioLevelIntegrated = fromMedia.AudioLevelIntegrated;
            AudioLevelPeak = fromMedia.AudioLevelPeak;
            Duration = fromMedia.Duration;
            DurationPlay = fromMedia.DurationPlay;
            TCStart = fromMedia.TCStart;
            TCPlay = fromMedia.TCPlay;
            VideoFormat = fromMedia.VideoFormat;
            MediaCategory = fromMedia.MediaCategory;
            Parental = fromMedia.Parental;
        }

        protected virtual Stream _getFileStream(bool forWrite)
        {
            return new FileStream(FullPath, forWrite ? FileMode.Create : FileMode.Open);
        }

       

        public virtual bool CopyMediaTo(Media destMedia, ref bool abortCopy)
        {
            bool copyResult = true;
            if (_directory.AccessType == TDirectoryAccessType.Direct && destMedia._directory.AccessType == TDirectoryAccessType.Direct)
            {
                File.Copy(FullPath, destMedia.FullPath, true);
                File.SetCreationTimeUtc(destMedia.FullPath, File.GetCreationTimeUtc(FullPath));
                File.SetLastWriteTimeUtc(destMedia.FullPath, File.GetLastWriteTimeUtc(FullPath));
            }
            else
            {
                try
                {
                    if (_directory is IngestDirectory)
                        (_directory as IngestDirectory).LockXDCAM(true);
                    if (destMedia._directory is IngestDirectory)
                        (destMedia._directory as IngestDirectory).LockXDCAM(true);
                    using (Stream source = _getFileStream(false),
                                    dest = destMedia._getFileStream(true))
                    {
                        var buffer = new byte[1024 * 1024];
                        ulong totalReadBytesCount = 0;
                        int readBytesCount;
                        FileSize = (UInt64)source.Length;
                        while ((readBytesCount = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (abortCopy)
                            {
                                copyResult = false;
                                break;
                            }
                            dest.Write(buffer, 0, readBytesCount);
                            totalReadBytesCount += (ulong)readBytesCount;
                            destMedia.FileSize = totalReadBytesCount;
                        }
                    }
                }
                finally
                {
                    if (_directory is IngestDirectory)
                        (_directory as IngestDirectory).LockXDCAM(false);
                    if (destMedia._directory is IngestDirectory)
                        (destMedia._directory as IngestDirectory).LockXDCAM(false);
                }
            }
            return copyResult;
        }
        
        public override string ToString()
        {
            return (!string.IsNullOrEmpty(MediaName)) ? MediaName : FileName;
        }

        protected virtual bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public bool FileExists()
        {
            return File.Exists(FullPath);
        }

        public bool MediaFileEqual(Media m)
        {
            return m.FileExists() 
                && this.FileExists()
                && m.FileSize == this.FileSize
                && m.FileName == this.FileName
                && m.LastUpdated == this.LastUpdated;
        }

        public void Remove()
        {
            Directory = null;
        }

        private bool _verified = false;
        public bool Verified
        {
            get { return _verified; }
            internal set { _verified = value; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal virtual void Verify()
        {
            if (Verified || (_mediaStatus == TMediaStatus.Copying) || (_mediaStatus == TMediaStatus.CopyPending || _mediaStatus == TMediaStatus.Required))
                return;
            if (!File.Exists(FullPath) && _directory != null && System.IO.Directory.Exists(_directory.Folder))
            {
                _mediaStatus = TMediaStatus.Deleted;
                return; // in case that no file was found, and directory exists
            }
            try
            {
                FileInfo fi = new FileInfo(FullPath);
                if (fi.Length == 0L)
                    return;
                if ((MediaType != TMediaType.AnimationFlash)
                    &&
                    (MediaStatus == TMediaStatus.Unknown
                    || MediaStatus == TMediaStatus.Deleted
                    || MediaStatus == TMediaStatus.Copied
                    || (MediaType != TMediaType.Still && Duration == TimeSpan.Zero)
                    || FileSize != (UInt64)fi.Length
                    || !LastUpdated.DateTimeEqualToDays(fi.LastWriteTimeUtc)
                    ))
                {
                    FileSize = (UInt64)fi.Length;
                    LastUpdated = DateTimeExtensions.FromFileTime(fi.LastWriteTimeUtc, DateTimeKind.Utc);
                    //this.LastAccess = DateTimeExtensions.FromFileTime(fi.LastAccessTimeUtc, DateTimeKind.Utc);
                    if (MediaGuid == Guid.Empty)
                        MediaGuid = Guid.NewGuid();
                    MediaChecker.Check(this);
                }
                if (MediaStatus == TMediaStatus.Available)
                    Directory.OnMediaVerified(this);
                Verified = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class MediaStatusEventArgs : EventArgs
    {
        public MediaStatusEventArgs(TMediaStatus NewStatus, TMediaStatus OldStatus)
        {
            newStatus = NewStatus;
            oldStatus = OldStatus;
        }
        public TMediaStatus newStatus { get; private set; }
        public TMediaStatus oldStatus { get; private set; }
    }

    public class MediaEventArgs : EventArgs
    {
        public MediaEventArgs(Media media)
        {
            Media = media;
        }
        public Media Media { get; private set; }
    }
}
