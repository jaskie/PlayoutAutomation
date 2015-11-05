using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Data;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class ArchiveMedia : PersistentMedia, IArchiveMedia
    {
        public ArchiveMedia(ArchiveDirectory directory) : base(directory) { }
        public ArchiveMedia(ArchiveDirectory directory, Guid guid) : base(directory, guid) { }
        public override bool Save()
        {
            if (MediaStatus == TMediaStatus.Available)
            {
                if (Modified)
                {
                    if (idPersistentMedia == 0)
                        return this.DbInsert();
                    else
                        return this.DbUpdate();
                }
            }
            if (MediaStatus == TMediaStatus.Deleted)
            {
                if (idPersistentMedia != 0)
                    return this.DbDelete();
            }
        return false;
        }
    }
}
