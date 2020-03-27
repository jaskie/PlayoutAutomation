using System;
using System.Collections.Generic;
using System.IO;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.MediaOperation;

namespace TAS.Server.Media
{
    public sealed class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectoryServerSide
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public IArchiveMedia Find(Guid mediaGuid)
        {
            return DatabaseProvider.Database.ArchiveMediaFind<ArchiveMedia>(this, mediaGuid);
        }

        public bool ContainsMedia(Guid mediaGuid)
        {
            return DatabaseProvider.Database.ArchiveContainsMedia(this, mediaGuid);
        }

        public event EventHandler<MediaIsArchivedEventArgs> MediaIsArchived;

        internal void ArchiveSave(ServerMedia media, bool deleteAfterSuccess)
        {
            var archived = DatabaseProvider.Database.ArchiveMediaFind<ArchiveMedia>(this, media.MediaGuid);
            if (archived != null)
                archived.Directory = this;

            if (deleteAfterSuccess && archived?.FileExists() == true)
            {
                FileManager.Current.Queue(
                    new DeleteOperation {Source = media});
                return;
            }
            if (archived?.FileExists() != true)
                _archiveCopy(media, this, deleteAfterSuccess);
        }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerDirectory destDirectory)
        {
            _archiveCopy((MediaBase)srcMedia, destDirectory, false);
        }

        public ulong IdArchive { get; set; }


        public override IMediaSearchProvider Search(TMediaCategory? category, string searchString)
        {
            return new MediaSearchProvider(DatabaseProvider.Database.ArchiveMediaSearch<ArchiveMedia>(this, category, searchString));
        }

        public void SweepStaleMedia()
        {
            IEnumerable<IMedia> staleMediaList = DatabaseProvider.Database.FindArchivedStaleMedia<ArchiveMedia>(this);
            foreach (var m in staleMediaList)
                m.Delete();
        }

        public override void RemoveMedia(IMedia media)
        {
            if (!(media is ArchiveMedia am))
                throw new ApplicationException("Media provided to RemoveMedia is not ArchiveMedia");
            base.RemoveMedia(media);
            MediaIsArchived?.Invoke(this, new MediaIsArchivedEventArgs(media, false));
            DatabaseProvider.Database.DeleteMedia(am);
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
            var operation = deleteAfterSuccess
                ? (FileOperationBase) new MoveOperation {Source = fromMedia, DestDirectory = destDirectory}
                : new CopyOperation {Source = fromMedia, DestDirectory = destDirectory};
            operation.Success += _archiveCopy_success;
            operation.Failure += _archiveCopy_failure;
            FileManager.Current.Queue(operation);
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
            if (sender is CopyOperation copyOperation)
                MediaIsArchived?.Invoke(this, new MediaIsArchivedEventArgs(copyOperation.Dest, true));
            operation.Success -= _archiveCopy_success;
            operation.Failure -= _archiveCopy_failure;
        }

        public override string ToString()
        {
            return "Archive";
        }

    }
}
