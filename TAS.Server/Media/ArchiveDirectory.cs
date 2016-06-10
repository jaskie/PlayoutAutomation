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

        public ArchiveDirectory(IMediaManager mediaManager, UInt64 id, string folder) : base((MediaManager)mediaManager)
        {
            idArchive = id;
            _folder = folder;
        }
        
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
            var m = media as ArchiveMedia;
            if (m != null)
            {
                try
                {
                    File.Delete(m.FullPath);
                }
                catch
                {
                    return false;
                }
                OnMediaDeleted(m);
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

        public IArchiveMedia GetArchiveMedia(IServerMedia media, bool searchExisting = true)
        {
            ArchiveMedia result = null;
            if (searchExisting)
                result = this.DbMediaFind<ArchiveMedia>(media);
            if (result == null)
            {
                string path = Path.Combine(Folder, GetCurrentFolder());
                result = new ArchiveMedia(this, media.MediaGuid, 0)
                {
                    FullPath = Path.Combine(path, FileUtils.GetUniqueFileName(path, media.FileName)),
                    MediaType = media.MediaType,
                };
                result.CloneMediaProperties(media);
            }
            return result;
        }

        public void ArchiveSave(IServerMedia media, bool deleteAfterSuccess)
        {
            ArchiveMedia archived;
            if (media.IsArchived 
                && (archived = this.DbMediaFind<ArchiveMedia>(media)) != null
                && archived.FileExists())
            {
                if (deleteAfterSuccess)
                    MediaManager.FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = media}, false);
            }
            else
            {
                IArchiveMedia toMedia = GetArchiveMedia(media);
                _archiveCopy((Media)media, (Media)toMedia, deleteAfterSuccess, false);
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
            if (!Directory.Exists(Path.GetDirectoryName(toMedia.FullPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(toMedia.FullPath));
            FileOperation operation = new FileOperation { Kind = deleteAfterSuccess ? TFileOperationKind.Move : TFileOperationKind.Copy, SourceMedia = fromMedia, DestMedia = toMedia };
            operation.Success += Archived;
            MediaManager.FileManager.Queue(operation, toTop);
        }

        private void Archived(object sender, EventArgs e)
        {
            var operation = sender as FileOperation;
            if (operation != null)
            {
                var sourceMedia = operation.SourceMedia as ServerMedia;
                if (sourceMedia != null)
                    sourceMedia.IsArchived = true;
                operation.Success -= Archived;
            }
        }
    }
}
