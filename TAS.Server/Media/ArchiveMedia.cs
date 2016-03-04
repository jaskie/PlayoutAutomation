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
        public ArchiveMedia(ArchiveDirectory directory) : base(directory) { }
        public ArchiveMedia(ArchiveDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid, idPersistentMedia) { }
        public override bool Save()
        {
            if (MediaStatus == TMediaStatus.Available)
            {
                if (Modified)
                {
                    if (IdPersistentMedia == 0)
                        return this.DbInsert();
                    else
                        return this.DbUpdate();
                }
            }
            if (MediaStatus == TMediaStatus.Deleted)
            {
                if (IdPersistentMedia != 0)
                    return this.DbDelete();
            }
        return false;
        }
    }
}
