using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IArchiveDirectory: IMediaDirectory, IArchiveDirectoryConfig
    {
        IArchiveMedia Find(IMedia media);
        IArchiveMedia GetArchiveMedia(IServerMedia media, bool searchExisting = true);
        void ArchiveSave(IServerMedia media, bool deleteAfterSuccess);
        void ArchiveRestore(IArchiveMedia srcMedia, IServerMedia destMedia, bool toTop);
        string SearchString { get; set; }
        TMediaCategory? SearchMediaCategory { get; set; }
        void Search();
    }
}
