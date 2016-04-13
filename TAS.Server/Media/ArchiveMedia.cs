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
                        result = this.DbInsert();
                    else
                    if (Modified)
                        result = this.DbUpdate();
                    Modified = false;
                }
            }
            return result;
        }
    }
}
