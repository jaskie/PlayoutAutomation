using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.Database;

namespace TAS.Server
{
    public class ArchiveDirectory : MediaDirectory, IArchiveDirectory
    {

        public ArchiveDirectory(MediaManager mediaManager) : base(mediaManager) { }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Initialize()
        {
            _isInitialized = false; // to avoid subsequent reinitializations
            DirectoryName = "Archiwum";
            GetVolumeInfo();
            Debug.WriteLine("ArchiveDirectory {0} initialized", Folder, null);
        }
        public UInt64 idArchive { get; set; }

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

        public override void Refresh()
        {
            Search();
        }

        public void Search()
        {
            this.Clear();
            this.DbSearch<ArchiveMedia>();
        }

        public TMediaCategory? SearchMediaCategory { get; set; }

        public override void SweepStaleMedia()
        {
            DateTime currentDate = DateTime.UtcNow.Date;
            IEnumerable<IMedia> StaleMediaList = this.DbFindStaleMedia<ArchiveMedia>();
            foreach (Media m in StaleMediaList)
                m.Delete();
        }

        public IArchiveMedia Find(IMedia media)
        {
            return this.DbMediaFind<ArchiveMedia>(media);
        }

        internal void Clear()
        {
            foreach (Media m in _files.Values.ToList())
                base.MediaRemove(m); //base: to not actually delete file and db
        }

        protected override IMedia CreateMedia(string fullPath, Guid guid = default(Guid))
        {
            throw new NotImplementedException();
        }

        //public override void MediaAdd(Media media)
        //{
        //    // do not add to _files
        //}

        public override bool DeleteMedia(IMedia media)
        {
            if (base.DeleteMedia(media))
            {
                MediaRemove(media);
                return true;
            }
            return false;
        }
        
        public override void MediaRemove(IMedia media)
        {
            ArchiveMedia m = (ArchiveMedia)media;
            m.MediaStatus = TMediaStatus.Deleted;
            m.Verified = false;
            m.Save();
            base.MediaRemove(media);
        }

        protected override void OnMediaRenamed(Media media, string newName)
        {
            ((ArchiveMedia)media).Save();
        }

        public IArchiveMedia GetArchiveMedia(IMedia media, bool searchExisting = true)
        {
            IArchiveMedia result = null;
            if (searchExisting)
                result = this.DbMediaFind<ArchiveMedia>(media);
            if (result == null)
                result = new ArchiveMedia(this, media.MediaGuid, 0)
                {
                    AudioChannelMapping = media.AudioChannelMapping,
                    AudioVolume = media.AudioVolume,
                    AudioLevelIntegrated = media.AudioLevelIntegrated,
                    AudioLevelPeak = media.AudioLevelPeak,
                    Duration = media.Duration,
                    DurationPlay = media.DurationPlay,
                    FullPath = Path.Combine(
                            Folder, 
                            GetCurrentFolder(), 
                            (media is IngestMedia) ? Path.GetFileNameWithoutExtension(media.FileName) + FileUtils.DefaultFileExtension(media.MediaType) : media.FileName),
                    FileSize = media.FileSize,
                    LastUpdated = media.LastUpdated,
                    MediaName = media.MediaName,
                    MediaStatus = TMediaStatus.Required,
                    TcStart = media.TcStart,
                    TcPlay = media.TcPlay,
                    VideoFormat = media.VideoFormat,
                    KillDate = (media is PersistentMedia) ? (media as PersistentMedia).KillDate : (media is IngestMedia ? media.LastUpdated + TimeSpan.FromDays(((IngestDirectory)media.Directory).MediaRetnentionDays) : default(DateTime)),
                    IdAux = (media is PersistentMedia) ? (media as PersistentMedia).IdAux : string.Empty,
                    IdProgramme = (media is PersistentMedia) ? (media as PersistentMedia).IdProgramme : 0L,
                    MediaType = (media.MediaType == TMediaType.Unknown) ? (FileUtils.StillFileTypes.Any(ve => ve == Path.GetExtension(media.FullPath).ToLowerInvariant()) ? TMediaType.Still : TMediaType.Movie) : media.MediaType,
                    MediaCategory = media.MediaCategory,
                    Parental = media.Parental,
                    OriginalMedia = media,
                };
            return result;
        }

        public void ArchiveSave(IMedia media, TVideoFormat outputFormat, bool deleteAfterSuccess)
        {
            IArchiveMedia toMedia = GetArchiveMedia(media);
            if (media is ServerMedia)
            {
                _archiveCopy((Media)media, (Media)toMedia, deleteAfterSuccess, false);
            }
            if (media is IngestMedia)
            {
                ConvertOperation operation = new ConvertOperation { SourceMedia = media, DestMedia = toMedia, OutputFormat = outputFormat };
                MediaManager.FileManager.Queue(operation, false);
            }
        }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerMedia destMedia, bool toTop)
        {
            if (destMedia != null)
                _archiveCopy((Media)srcMedia, (Media)destMedia, false, toTop);
        }

        internal string GetCurrentFolder()
        {
            return DateTime.UtcNow.ToString("yyyyMM"); 
        }

        private void _archiveCopy(Media fromMedia, Media toMedia, bool deleteAfterSuccess, bool toTop)
        {
            if (fromMedia.MediaGuid == toMedia.MediaGuid && fromMedia.FilePropertiesEqual(toMedia))
            {
                if (deleteAfterSuccess)
                {
                    FileOperation operation;
                    operation = new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = fromMedia };
                    MediaManager.FileManager.Queue(operation, toTop);
                }
            }
            else
            {
                if (!Directory.Exists(Path.GetDirectoryName(toMedia.FullPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(toMedia.FullPath));
                MediaManager.FileManager.Queue(new FileOperation { Kind = deleteAfterSuccess ? TFileOperationKind.Move : TFileOperationKind.Copy, SourceMedia = fromMedia, DestMedia = toMedia }, toTop);
            }
        }    

    }
}
