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
            DirectoryName = "Archiwum";
            GetVolumeInfo();
            Search();
            IsInitialized = true;
            Debug.WriteLine("ArchiveDirectory {0} initialized", Folder, null);
        }
        public UInt64 idArchive { get; set; }

        private string _searchString = string.Empty;
        public string SearchString
        {
            get { return _searchString; }
            set { SetField(ref _searchString, value); }
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

        public IArchiveMedia Find(IMediaProperties media)
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
            m.IsVerified = false;
            m.Save();
            base.MediaRemove(media);
        }

        protected override void OnMediaRenamed(Media media, string newName)
        {
            ((ArchiveMedia)media).Save();
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            string path = Path.Combine(Folder, GetCurrentFolder());
            var result = new ArchiveMedia(this, mediaProperties.MediaGuid, 0)
            {
                FullPath = Path.Combine(path, FileUtils.GetUniqueFileName(path, mediaProperties.FileName)),
            };
            result.CloneMediaProperties(mediaProperties);
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
                _archiveCopy((Media)media, this, deleteAfterSuccess, false);
        }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerDirectory destDirectory, bool toTop)
        {
                _archiveCopy((Media)srcMedia, destDirectory, false, toTop);
        }

        internal string GetCurrentFolder()
        {
            return DateTime.UtcNow.ToString("yyyyMM"); 
        }

        private void _archiveCopy(Media fromMedia, IMediaDirectory destDirectory, bool deleteAfterSuccess, bool toTop)
        {
            FileOperation operation = new FileOperation { Kind = deleteAfterSuccess ? TFileOperationKind.Move : TFileOperationKind.Copy, SourceMedia = fromMedia, DestDirectory = destDirectory };
            operation.Success += _archived;
            operation.Failure += _failure;
            MediaManager.FileManager.Queue(operation, toTop);
        }

        private void _failure(object sender, EventArgs e)
        {
            var operation = sender as FileOperation;
            if (operation != null)
            {
                operation.Success -= _archived;
                operation.Failure -= _failure;
            }
        }

        private void _archived(object sender, EventArgs e)
        {
            var operation = sender as FileOperation;
            if (operation != null)
            {
                var sourceMedia = operation.SourceMedia as ServerMedia;
                if (sourceMedia != null)
                    sourceMedia.IsArchived = true;
                operation.Success -= _archived;
                operation.Failure -= _failure;
            }
        }


    }
}
