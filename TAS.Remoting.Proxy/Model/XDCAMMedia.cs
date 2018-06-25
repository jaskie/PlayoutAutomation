using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class XDCAMMedia : IngestMedia, IXdcamMedia
    {
#pragma warning disable CS0649
        [JsonProperty(nameof(IXdcamMedia.ClipNr))]
        private int _clipNr;
#pragma warning restore

        public int ClipNr => _clipNr;
    }
}
