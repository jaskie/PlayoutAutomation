using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.MediaOperation;

namespace TAS.Server.Media
{
    public sealed class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectoryServerSide
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public IArchiveMedia Find(IMediaProperties media)
        {
            return EngineController.Database.ArchiveMediaFind<ArchiveMedia>(this, media.MediaGuid);
        }

        internal void ArchiveSave(ServerMedia media, bool deleteAfterSuccess)
        {
            if (media.IsArchived)
            {
                var archived = EngineController.Database.ArchiveMediaFind<ArchiveMedia>(this, media.MediaGuid);
                if (archived?.FileExists() == true)
                {
                    if (deleteAfterSuccess)
                        MediaManager.FileManager.Queue(
                            new DeleteOperation((FileManager) MediaManager.FileManager) {Source = media});
                }
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
            if (((ServerDirectory) MediaManager.MediaDirectoryPRI)?.FindMediaByMediaGuid(am.MediaGuid) is ServerMedia mediaPgm)
                mediaPgm.IsArchived = false;
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
            var fileManager = (FileManager)MediaManager.FileManager;
            FileOperationBase operation = deleteAfterSuccess
                ? (FileOperationBase) new MoveOperation(fileManager) {Source = fromMedia, DestDirectory = destDirectory}
                : new CopyOperation(fileManager) {Source = fromMedia, DestDirectory = destDirectory};
            operation.Success += _archiveCopy_success;
            operation.Failure += _archiveCopy_failure;
            MediaManager.FileManager.Queue(operation);
        }

        private void _archiveCopy_failure(object sender, EventArgs e)
        {
            if (!(sender is CopyOperation operation))
                return;
            operation.Success -= _archiveCopy_success;
            operation.Failure -= _archiveCopy_failure;
        }

        private void _archiveCopy_success(object sender, EventArgs e)
        {
            if (!(sender is FileOperationBase operation))
                return;
            if (sender is CopyOperation copyOperation &&  copyOperation.Source is ServerMedia serverMedia)
                serverMedia.IsArchived = true;
            operation.Success -= _archiveCopy_success;
            operation.Failure -= _archiveCopy_failure;
        }
    }
}
