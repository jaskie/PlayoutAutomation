using System;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public class IngestMedia: MediaBase, IIngestMedia
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IIngestMedia.GetIngestStatus))]
        private TIngestStatus _ingestStatus;

        private readonly Lazy<TIngestStatus> _ingestStatusLazy;
        #pragma warning restore

        public IngestMedia()
        {
            _ingestStatusLazy = new Lazy<TIngestStatus>(() =>
            {
                _ingestStatus = Get<TIngestStatus>(nameof(IngestStatus));
                return _ingestStatus;
            });
        }

        public TIngestStatus IngestStatus => _ingestStatusLazy.IsValueCreated ? _ingestStatus : _ingestStatusLazy.Value;
        public TIngestStatus GetIngestStatus(IServerDirectory directory)
        {
            return Query<TIngestStatus>(parameters: new object[] {directory});
        }
    }
}
