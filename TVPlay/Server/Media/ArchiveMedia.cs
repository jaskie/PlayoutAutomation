using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server
{
    public class ArchiveMedia : PersistentMedia
    {
        public override bool Save()
        {
            if (MediaStatus == TMediaStatus.Available)
            {
                if (Modified)
                {
                    if (idPersistentMedia == 0)
                        return DatabaseConnector.ArchiveMediaInsert(this);
                    else
                        return DatabaseConnector.ArchiveMediaUpdate(this);
                }
            }
            if (MediaStatus == TMediaStatus.Deleted)
            {
                if (idPersistentMedia != 0)
                    return DatabaseConnector.ArchiveMediaDelete(this);
            }
        return false;
        }
    }
}
