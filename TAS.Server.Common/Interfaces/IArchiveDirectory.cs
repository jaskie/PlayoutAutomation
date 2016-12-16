using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IArchiveDirectory: IMediaDirectory, IArchiveDirectoryProperties
    {
        IArchiveMedia Find(IMediaProperties media);
        void ArchiveSave(IServerMedia media, bool deleteAfterSuccess);
        void ArchiveRestore(IArchiveMedia srcMedia, IServerDirectory destDirectory, bool toTop);
        string SearchString { get; set; }
        TMediaCategory? SearchMediaCategory { get; set; }
        void Search();
    }

    public interface IArchiveDirectoryProperties
    {
        ulong idArchive { get; set; }
        string Folder { get; set; }
    }
}
