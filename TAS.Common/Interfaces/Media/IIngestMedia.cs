using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces.Media
{
    public interface IIngestMedia : IMedia
    {
        TIngestStatus IngestStatus(IServerDirectory directory);
    }
}
