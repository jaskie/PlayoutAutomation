using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Database;

namespace TAS.Server
{
    public class ArchiveMedia : PersistentMedia, IArchiveMedia
    {
        public ArchiveMedia(IArchiveDirectory directory) : base(directory) { }
        public ArchiveMedia(IArchiveDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid, idPersistentMedia) { }

        private TIngestStatus _ingestStatus;
        public TIngestStatus IngestStatus
        {
            get
            {
                if (_ingestStatus == TIngestStatus.Unknown)
                {
                    var sdir = _directory.MediaManager.MediaDirectoryPRI as ServerDirectory;
                    if (sdir != null)
                    {
                        var media = sdir.FindMediaByMediaGuid(_mediaGuid);
                        if (media != null && media.MediaStatus == TMediaStatus.Available)
                            _ingestStatus = TIngestStatus.Ready;
                    }
                }
                return _ingestStatus;
            }
            internal set { SetField(ref _ingestStatus, value, "IngestStatus"); }
        }

        public override bool Save()
        {
            bool result = false;
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
                    if (Modified)
                        result = this.DbUpdate(((ArchiveDirectory)_directory).idArchive);
                    Modified = false;
                }
            }
            return result;
        }
    }
}
