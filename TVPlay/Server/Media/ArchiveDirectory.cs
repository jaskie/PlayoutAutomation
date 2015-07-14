using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;
using TAS.Common;

namespace TAS.Server
{
    public class ArchiveDirectory : MediaDirectory
    {
        public override void Initialize()
        {
            _isInitialized = false; // to avoid subsequent reinitializations
            DirectoryName = "Archiwum";
            _getVolumeInfo();
            Debug.WriteLine("ArchiveDirectory {0} initialized", Folder, null);
        }
        public UInt64 IdArchive { get; internal set; }

        private string _searchString;
        public string SearchString
        {
            get { return _searchString; }
            set
            {
                if (_searchString != value)
                {
                    _searchString = value;
                    NotifyPropertyChanged("SearchString");
                }
            }
        }

        public void Search()
        {
            DatabaseConnector.ArchiveMediaSearch(this);
        }

        public TMediaCategory? SearchMediaCategory { get; set; }

        public override void SweepStaleMedia()
        {
            DateTime currentDate = DateTime.UtcNow.Date;
            DatabaseConnector.ArchiveMediaFindStaleMedia(this);
            IEnumerable<Media> StaleMediaList;
            _files.Lock.EnterReadLock();
            try
            {
                StaleMediaList = _files.Where(m => (m is ArchiveMedia) && currentDate > (m as ArchiveMedia).KillDate);
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
            foreach (Media m in StaleMediaList)
                m.Delete();
        }

        public ArchiveMedia Find(Media media)
        {
            return DatabaseConnector.ArchiveMediaFind(media, this);
        }

        internal void Clear()
        {
            _files.Lock.EnterUpgradeableReadLock();
            try
            {
                foreach (Media m in _files.ToList())
                    base.MediaRemove(m); //base: to not actually delete file and db
            }
            finally
            {
                _files.Lock.ExitUpgradeableReadLock();
            }
                 
        }

        protected override Media CreateMedia()
        {
            return new ArchiveMedia() { Directory = this, };
        }

        //public override void MediaAdd(Media media)
        //{
        //    // do not add to _files
        //}

        public override bool DeleteMedia(Media media)
        {
            if (base.DeleteMedia(media))
            {
                MediaRemove(media);
                return true;
            }
            return false;
        }
        
        public override void MediaRemove(Media media)
        {
            ArchiveMedia m = (ArchiveMedia)media;
            m.MediaStatus = TMediaStatus.Deleted;
            m.Verified = false;
            m.Save();
            base.MediaRemove(media);
        }

        protected override void OnMediaRenamed(Media media, string newName)
        {
            base.OnMediaRenamed(media, newName);
            ((ArchiveMedia)media).Save();
        }

        public ArchiveMedia GetArchiveMedia(Media media, bool searchExisting = true)
        {
            ArchiveMedia result = null;
            if (searchExisting)
                result = DatabaseConnector.ArchiveMediaFind(media, this);
            if (result == null)
                result = new ArchiveMedia()
                {
                    _audioChannelMapping = media.AudioChannelMapping,
                    _audioVolume = media.AudioVolume,
                    _audioLevelIntegrated = media.AudioLevelIntegrated,
                    _audioLevelPeak = media.AudioLevelPeak,
                    _duration = media.Duration,
                    _durationPlay = media.DurationPlay,
                    _fileName = (media is IngestMedia) ? Path.GetFileNameWithoutExtension(media.FileName) + DefaultFileExtension(media.MediaType) : media.FileName,
                    _fileSize = media.FileSize,
                    _folder = GetCurrentFolder(),
                    _lastUpdated = media.LastUpdated,
                    _mediaName = media.MediaName,
                    _mediaStatus = TMediaStatus.Required,
                    _tCStart = media.TCStart,
                    _tCPlay = media.TCPlay,
                    _videoFormat = media.VideoFormat,
                    KillDate = (media is PersistentMedia) ? (media as PersistentMedia).KillDate : (media is IngestMedia ? media.LastUpdated + TimeSpan.FromDays(((IngestDirectory)media.Directory).MediaRetnentionDays) : default(DateTime)),
                    idAux = (media is PersistentMedia) ? (media as PersistentMedia).idAux : string.Empty,
                    idFormat = (media is PersistentMedia) ? (media as PersistentMedia).idFormat : 0L,
                    idProgramme = (media is PersistentMedia) ? (media as PersistentMedia).idProgramme : 0L,
                    MediaType = (media.MediaType == TMediaType.Unknown) ? (StillFileTypes.Any(ve => ve == Path.GetExtension(media.FullPath).ToLowerInvariant()) ? TMediaType.Still : TMediaType.Movie) : media.MediaType,
                    _mediaCategory = media.MediaCategory,
                    _parental = media.Parental,
                    _mediaGuid = media.MediaGuid,
                    OriginalMedia = media,
                    Directory = this,
                };
            return result;
        }

        public void ArchiveSave(Media media, TVideoFormat outputFormat, bool deleteAfterSuccess)
        {
            ArchiveMedia toMedia = GetArchiveMedia(media);
            if (media is ServerMedia)
            {
                _archiveCopy(media, toMedia, deleteAfterSuccess, false);
            }
            if (media is IngestMedia)
            {
                FileManager.Queue(new ConvertOperation { SourceMedia = media, DestMedia = toMedia, SuccessCallback = _getVolumeInfo, OutputFormat = outputFormat});
            }
        }

        public void ArchiveRestore(ArchiveMedia media, ServerMedia mediaPGM, bool toTop)
        {
            if (mediaPGM != null)
                _archiveCopy(media, mediaPGM, false, toTop);
        }

        internal string GetCurrentFolder()
        {
            return DateTime.UtcNow.ToString("yyyyMM"); 
        }

        private void _archiveCopy(Media fromMedia, Media toMedia, bool deleteAfterSuccess, bool toTop)
        {
            if (fromMedia.MediaGuid == toMedia.MediaGuid && fromMedia.MediaFileEqual(toMedia))
            {
                if (deleteAfterSuccess)
                    FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = fromMedia, SuccessCallback = _getVolumeInfo}, toTop);
            }
            else
            {
                if (!Directory.Exists(Path.GetDirectoryName(toMedia.FullPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(toMedia.FullPath));
                FileManager.Queue(new FileOperation { Kind = deleteAfterSuccess ? TFileOperationKind.Move : TFileOperationKind.Copy, SourceMedia = fromMedia, DestMedia = toMedia, SuccessCallback = _getVolumeInfo }, toTop);
            }
        }    

    }
}
