using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Database.Interfaces.Media;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class ArchiveMedia : PersistentMedia, Common.Database.Interfaces.Media.IArchiveMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ArchiveMedia));
        private TIngestStatus _ingestStatus;

        ~ArchiveMedia()
        {
            Debug.WriteLine("ArchiveMedia finalized");    
        }

        [JsonProperty]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Database.ArchiveMediaFieldLengths;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public TIngestStatus IngestStatus
        {
            get
            {
                if (_ingestStatus != TIngestStatus.Unknown) return _ingestStatus;
                var sdir = (Directory as MediaDirectoryBase)?.MediaManager.MediaDirectoryPRI as ServerDirectory;
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
                            result = EngineController.Database.DeleteMedia(this);
                    }
                    else
                    {
                        if (IdPersistentMedia == 0)
                            result = EngineController.Database.InsertMedia(this, ((ArchiveDirectory)Directory).IdArchive);
                        else if (IsModified)
                        {
                            EngineController.Database.UpdateMedia(this, ((ArchiveDirectory) Directory).IdArchive);
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
