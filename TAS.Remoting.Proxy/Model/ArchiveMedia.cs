using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class ArchiveMedia: PersistentMedia, IArchiveMedia
    {
        #pragma warning disable CS0649 
     
        [JsonProperty(nameof(IArchiveMedia.IngestStatus))]
        private TIngestStatus _ingestStatus;

        #pragma warning restore

        public TIngestStatus IngestStatus => _ingestStatus;
    }
}
