using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IArchiveDirectory: ISearchableDirectory
    {
        IArchiveMedia Find(IMediaProperties media);
    }

    public interface IArchiveDirectoryProperties: IMediaDirectoryProperties
    {
        ulong IdArchive { get; set; }
    }

    public interface IArchiveDirectoryServerSide : IMediaDirectoryServerSide, IArchiveDirectory, IArchiveDirectoryProperties
    {
        IMediaManager MediaManager { get; set; }
    }

}
