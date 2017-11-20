using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class XDCAMMedia : IngestMedia, IXdcamMedia
    {
        public int ClipNr
        {
            get { return Get<int>(); }
            set { SetLocalValue(value); }
        }
    }
}
