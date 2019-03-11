using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public sealed class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectoryServerSide
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ArchiveDirectory()
        {
            DirectoryName = "Archive"; //TODO: localize
        }

        public IArchiveMedia Find(IMediaProperties media)
        {
            return EngineController.Database.ArchiveMediaFind<ArchiveMedia>(this, media.MediaGuid);
        }

        internal void ArchiveSave(ServerMedia media, bool deleteAfterSuccess)
        {
            ArchiveMedia archived;
            if (media.IsArchived
                && (archived = EngineController.Database.ArchiveMediaFind<ArchiveMedia>(this, media.MediaGuid)) != null
                && archived.FileExists())
            {
                if (deleteAfterSuccess)
                    MediaManager.FileManager.Queue(new FileOperation((FileManager)MediaManager.FileManager) { Kind = TFileOperationKind.Delete, Source = media });
            }
            else
                _archiveCopy(media, this, deleteAfterSuccess);
        }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerDirectory destDirectory)
        {
            _archiveCopy((MediaBase)srcMedia, destDirectory, false);
        }

        public ulong IdArchive { get; set; }

        public List<IMedia> Search(TMediaCategory? category, string searchString)
        {
            return EngineController.Database.ArchiveMediaSearch<ArchiveMedia>(this, category, searchString).ToList<IMedia>();
        }

        public void SweepStaleMedia()
        {
            IEnumerable<IMedia> staleMediaList = EngineController.Database.FindArchivedStaleMedia<ArchiveMedia>(this);
            foreach (var m in staleMediaList)
                m.Delete();
        }

        public override void RemoveMedia(IMedia media)
        {
            if (!(media is ArchiveMedia am))
                throw new ApplicationException("Media provided to RemoveMedia is not ArchiveMedia");
            am.MediaStatus = TMediaStatus.Deleted;
            am.IsVerified = false;
            am.Save();
        }

        internal override IMedia CreateMedia(IMediaProperties media)
        {
            var newFileName = media.FileName;
            if (File.Exists(Path.Combine(Folder, newFileName)))
            {
                Logger.Trace("{0}: File {1} already exists", nameof(CreateMedia), newFileName);
                newFileName = FileUtils.GetUniqueFileName(Folder, newFileName);
            }
            var result = new ArchiveMedia
            {
                MediaName = media.MediaName,
                MediaGuid = media.MediaGuid,
                LastUpdated = media.LastUpdated,
                MediaType = media.MediaType,
                Folder = GetCurrentFolder(),
                FileName = newFileName,
                MediaStatus = TMediaStatus.Required,
            };
            result.CloneMediaProperties(media);
            AddMedia(result);
            return result;
        }

        internal string GetCurrentFolder()
        {
            return DateTime.UtcNow.ToString("yyyyMM");
        }

        private void _archiveCopy(IMedia fromMedia, IMediaDirectory destDirectory, bool deleteAfterSuccess)
        {
            var operation = new FileOperation((FileManager)MediaManager.FileManager) { Kind = deleteAfterSuccess ? TFileOperationKind.Move : TFileOperationKind.Copy, Source = fromMedia, DestDirectory = destDirectory };
            operation.Success += _archiveCopy_success;
            operation.Failure += _archiveCopy_failure;
            MediaManager.FileManager.Queue(operation);
        }

        private void _archiveCopy_failure(object sender, EventArgs e)
        {
            if (!(sender is FileOperation operation))
                return;
            operation.Success -= _archiveCopy_success;
            operation.Failure -= _archiveCopy_failure;
        }

        private void _archiveCopy_success(object sender, EventArgs e)
        {
            if (!(sender is FileOperation operation))
                return;
            if (operation.Source is ServerMedia sourceMedia)
                sourceMedia.IsArchived = true;
            operation.Success -= _archiveCopy_success;
            operation.Failure -= _archiveCopy_failure;
        }
    }
}
