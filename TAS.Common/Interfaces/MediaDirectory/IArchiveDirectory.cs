using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IArchiveDirectory: IArchiveDirectoryProperties, ISearchableDirectory
    {
        IArchiveMedia Find(IMediaProperties media);
    }

    public interface IArchiveDirectoryProperties: IMediaDirectory
    {
        ulong IdArchive { get; set; }
    }
}
