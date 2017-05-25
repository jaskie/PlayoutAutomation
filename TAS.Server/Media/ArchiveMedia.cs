using System;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ArchiveMedia : PersistentMedia, IArchiveMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ArchiveMedia));
        private TIngestStatus _ingestStatus;

        public ArchiveMedia(IArchiveDirectory directory, Guid guid, ulong idPersistentMedia) : base(directory, guid, idPersistentMedia) { }

        public TIngestStatus IngestStatus
        {
            get
            {
                if (_ingestStatus != TIngestStatus.Unknown) return _ingestStatus;
                var sdir = _directory.MediaManager.MediaDirectoryPRI as ServerDirectory;
                var media = sdir?.FindMediaByMediaGuid(_mediaGuid);
                if (media != null && media.MediaStatus == TMediaStatus.Available)
                    _ingestStatus = TIngestStatus.Ready;
                return _ingestStatus;
            }
            set { SetField(ref _ingestStatus, value); }
        }

        public override bool Save()
        {
            bool result = false;
            try
            {
                if (MediaStatus != TMediaStatus.Unknown)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            result = this.DbDelete();
                    }
                    else
                    {
                        if (IdPersistentMedia == 0)
                            result = this.DbInsert(((ArchiveDirectory)_directory).idArchive);
                        else
                        if (IsModified)
                            result = this.DbUpdate(((ArchiveDirectory)_directory).idArchive);
                        IsModified = false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            return result;
        }
    }
}
