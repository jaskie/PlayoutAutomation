using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IArchiveDirectory: IArchiveDirectoryProperties, ISearchableDirectory
    {
        IArchiveMedia Find(IMediaProperties media);

        IMediaManager MediaManager { get; set; }
    }

    public interface IArchiveDirectoryProperties: IMediaDirectory
    {
        ulong IdArchive { get; set; }
    }

    public interface IArchiveDirectoryServerSide : IMediaDirectoryServerSide, IArchiveDirectoryProperties
    {
        
    }

}
