using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces.Media
{
    public interface IServerMedia: IPersistentMedia, IServerMediaProperties
    {
        bool GetIsArchived(IArchiveDirectoryProperties directory);
    }

    public interface IServerMediaProperties: IPersistentMediaProperties
    {
        bool DoNotArchive { get; set; }
    }
}
