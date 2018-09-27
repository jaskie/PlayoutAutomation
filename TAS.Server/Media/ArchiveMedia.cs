using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ArchiveMedia : PersistentMedia, IArchiveMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ArchiveMedia));
        private TIngestStatus _ingestStatus;

        [JsonProperty]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Database.ArchiveMediaFieldLengths;
        
        public TIngestStatus IngestStatus
        {
            get
            {
                if (_ingestStatus != TIngestStatus.Unknown) return _ingestStatus;
                var sdir = ((ArchiveDirectory)Directory).MediaManager.MediaDirectoryPRI as ServerDirectory;
                var media = sdir?.FindMediaByMediaGuid(MediaGuid);
                if (media != null && media.MediaStatus == TMediaStatus.Available)
                    _ingestStatus = TIngestStatus.Ready;
                return _ingestStatus;
            }
            set => SetField(ref _ingestStatus, value);
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
                            result = EngineController.Database.DbDeleteMedia(this);
                    }
                    else
                    {
                        if (IdPersistentMedia == 0)
                            result = EngineController.Database.DbInsertMedia(this, ((ArchiveDirectory)Directory).idArchive);
                        else if (IsModified)
                        {
                            EngineController.Database.DbUpdateMedia(this, ((ArchiveDirectory) Directory).idArchive);
                            result = true;
                        }
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
