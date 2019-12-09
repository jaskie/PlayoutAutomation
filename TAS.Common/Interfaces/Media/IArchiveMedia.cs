using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces.Media
{
    public interface IArchiveMedia: IPersistentMedia
    {
        TIngestStatus GetIngestStatus(IServerDirectory directory);
    }
}
