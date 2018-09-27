using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ArchiveDirectory : MediaDirectory, IArchiveDirectoryServerSide
    {
        private string _searchString = string.Empty;

        internal ArchiveDirectory(IMediaManager mediaManager, UInt64 id, string folder) : base((MediaManager)mediaManager)
        {
            idArchive = id;
            Folder = folder;
        }

        public IArchiveMedia Find(IMediaProperties media)
        {
            return EngineController.Database.DbMediaFind<ArchiveMedia>(this, media);
        }

        internal void ArchiveSave(ServerMedia media, bool deleteAfterSuccess)
        {
            ArchiveMedia archived;
            if (media.IsArchived
                && (archived = EngineController.Database.DbMediaFind<ArchiveMedia>(this, media)) != null
                && archived.FileExists())
            {
                if (deleteAfterSuccess)
                {
                    MediaManager.FileManager.Queue(new FileOperation((FileManager)MediaManager.FileManager) { Kind = TFileOperationKind.Delete, Source = media },
                        false);
                }
            }
            else
                _archiveCopy(media, this, deleteAfterSuccess, false);
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
            EngineController.Database.DbSearch<ArchiveMedia>(this);
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
            IEnumerable<IMedia> staleMediaList = EngineController.Database.FindStaleMedia<ArchiveMedia>(this);
            foreach (var m in staleMediaList)
                m.Delete();
        }

        public override bool DeleteMedia(IMedia media)
        {
            if (!(media is ArchiveMedia m))
                return false;
            try
            {
                File.Delete(m.FullPath);
            }
            catch
            {
                // ignored
            }
            NotifyMediaDeleted(m);
            RemoveMedia(media);
            return true;
        }

        public override void RemoveMedia(IMedia media)
        {
            if (!(media is ArchiveMedia am))
                throw new ApplicationException("Media provided to RemoveMedia is not ArchiveMedia");
            am.MediaStatus = TMediaStatus.Deleted;
            am.IsVerified = false;
            am.Save();
            base.RemoveMedia(am);
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            var newFileName = mediaProperties.FileName;
            if (File.Exists(Path.Combine(Folder, newFileName)))
            {
                Logger.Trace("{0}: File {1} already exists", nameof(CreateMedia), newFileName);
                newFileName = FileUtils.GetUniqueFileName(Folder, newFileName);
            }
            var result = new ArchiveMedia
            {
                MediaName = mediaProperties.MediaName,
                MediaGuid = FindMediaByMediaGuid(mediaProperties.MediaGuid)?.MediaGuid ?? Guid.NewGuid(),
                LastUpdated = mediaProperties.LastUpdated,
                MediaType = mediaProperties.MediaType == TMediaType.Unknown ? TMediaType.Movie : mediaProperties.MediaType,
                Folder = GetCurrentFolder(),
                FileName = newFileName,
                MediaStatus = TMediaStatus.Required,
            };
            result.CloneMediaProperties(mediaProperties);
            return result;
        }

        internal void Clear()
        {
            foreach (var m in GetFiles())
                base.RemoveMedia(m); //base: to not actually delete file and db
        }

        internal string GetCurrentFolder()
        {
            return DateTime.UtcNow.ToString("yyyyMM");
        }

        protected override IMedia CreateMedia(string fullPath, string mediaName, DateTime lastUpdated, TMediaType mediaType, Guid guid = default(Guid))
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
