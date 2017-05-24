using TAS.Server.Common;

namespace TAS.Server.Media
{
    interface IServerIngestStatusMedia
    {
        TIngestStatus IngestStatus { get;  set; }
    }
}
