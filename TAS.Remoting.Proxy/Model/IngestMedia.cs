using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class IngestMedia: MediaBase, IIngestMedia
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IIngestMedia.IngestStatus))]
        private TIngestStatus _ingestStatus;

        #pragma warning restore

        public TIngestStatus IngestStatus => _ingestStatus;
    }
}
