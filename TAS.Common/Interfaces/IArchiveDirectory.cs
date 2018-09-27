using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Common.Interfaces
{
    public interface IArchiveDirectory: IMediaDirectory, IArchiveDirectoryProperties
    {
        IArchiveMedia Find(IMediaProperties media);
        string SearchString { get; set; }
        TMediaCategory? SearchMediaCategory { get; set; }
        void Search();
    }

    public interface IArchiveDirectoryProperties: IMediaDirectoryProperties
    {
        ulong idArchive { get; set; }
    }

    public interface IArchiveDirectoryServerSide : IArchiveDirectory, IMediaDirectoryServerSide
    {
        
    }
}
