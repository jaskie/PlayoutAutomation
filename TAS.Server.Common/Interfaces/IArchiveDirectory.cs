using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IArchiveDirectory: IMediaDirectory
    {
        IArchiveMedia Find(IMedia media);
        IArchiveMedia GetArchiveMedia(IMedia media, bool searchExisting = true);
        void ArchiveSave(IMedia media, TVideoFormat outputFormat, bool deleteAfterSuccess);
        void ArchiveRestore(IArchiveMedia media, IServerMedia mediaPGM, bool toTop);

    }
}
