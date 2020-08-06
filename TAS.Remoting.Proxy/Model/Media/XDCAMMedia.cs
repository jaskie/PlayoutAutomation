using jNet.RPC;
using TAS.Common.Interfaces.Media;

namespace TAS.Remoting.Model.Media
{
    public class XdcamMedia : IngestMedia, IXdcamMedia
    {
#pragma warning disable CS0649
        [DtoMember(nameof(IXdcamMedia.ClipNr))]
        private int _clipNr;
#pragma warning restore

        public int ClipNr => _clipNr;
    }
}
