using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IArchiveDirectory: IArchiveDirectoryProperties
    {
        IArchiveMedia Find(IMediaProperties media);

        List<IArchiveMedia> Search(TMediaCategory? category, string searchString);
    }

    public interface IArchiveDirectoryProperties: IMediaDirectory
    {
        ulong IdArchive { get; set; }
    }
}
