using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class IngestMedia: MediaBase, IIngestMedia
    {
        public TIngestStatus IngestStatus { get { return Get<TIngestStatus>(); } set { SetLocalValue(value); } }
    }
}
