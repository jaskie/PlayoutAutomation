using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class XDCAMMedia : IngestMedia, IXdcamMedia
    {
        [JsonProperty(nameof(IXdcamMedia.ClipNr))]
        private int _clipNr;
        public int ClipNr => _clipNr;
    }
}
