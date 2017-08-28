using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ArchiveDirectory : MediaDirectory, IArchiveDirectory
    {
        private string _searchString = string.Empty;

        internal ArchiveDirectory(IMediaManager mediaManager, UInt64 id, string folder) : base((MediaManager)mediaManager)
        {
            idArchive = id;
            Folder = folder;
        }

        public IArchiveMedia Find(IMediaProperties media)
        {
            return this.DbMediaFind<ArchiveMedia>(media);
        }

        public void ArchiveSave(IServerMedia media, bool deleteAfterSuccess)
        {
            ArchiveMedia archived;
            if (media.IsArchived
                && (archived = this.DbMediaFind<ArchiveMedia>(media)) != null
                && archived.FileExists())
            {
                if (deleteAfterSuccess)
                {
                    MediaManager.FileManager.Queue(new FileOperation((FileManager)MediaManager.FileManager) { Kind = TFileOperationKind.Delete, Source = media },
                        false);
                }
            }
            else
                _archiveCopy((MediaBase)media, this, deleteAfterSuccess, false);
        }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerDirectory destDirectory, bool toTop)
        {
            _archiveCopy((MediaBase)srcMedia, destDirectory, false, toTop);
        }

        public ulong idArchive { get; set; }

        public string SearchString
        {
            get { return _searchString; }
            set { SetField(ref _searchString, value); }
        }

        public void Search()
        {
            Clear();
            this.DbSearch<ArchiveMedia>();
        }

        public TMediaCategory? SearchMediaCategory { get; set; }

        public override void Initialize()
        {
            DirectoryName = "Archiwum";
            GetVolumeInfo();
            Search();
            IsInitialized = true;
            Debug.WriteLine(this, $"ArchiveDirectory {Folder} initialized");
        }

        public override void Refresh()
        {
            Search();
        }

        public override void SweepStaleMedia()
        {
            IEnumerable<IMedia> staleMediaList = this.DbFindStaleMedia<ArchiveMedia>();
            foreach (var m in staleMediaList)
                m.Delete();
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
                NotifyMediaDeleted(m);
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

        internal void Clear()
        {
            foreach (var m in GetFiles())
                base.MediaRemove(m); //base: to not actually delete file and db
        }

        internal string GetCurrentFolder()
        {
            return DateTime.UtcNow.ToString("yyyyMM");
        }

        protected override IMedia CreateMedia(string fullPath, Guid guid = default(Guid))
        {
            throw new NotImplementedException();
        }

        protected override void OnMediaRenamed(MediaBase media, string newName)
        {
            ((ArchiveMedia)media).Save();
        }

        private void _archiveCopy(MediaBase fromMedia, IMediaDirectory destDirectory, bool deleteAfterSuccess, bool toTop)
        {
            FileOperation operation = new FileOperation((FileManager)MediaManager.FileManager) { Kind = deleteAfterSuccess ? TFileOperationKind.Move : TFileOperationKind.Copy, Source = fromMedia, DestDirectory = destDirectory };
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
                var sourceMedia = operation.Source as ServerMedia;
                if (sourceMedia != null)
                    sourceMedia.IsArchived = true;
                operation.Success -= _archived;
                operation.Failure -= _failure;
            }
        }

    }
}
