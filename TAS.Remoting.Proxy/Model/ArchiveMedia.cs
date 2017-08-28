using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class ArchiveMedia: PersistentMedia, IArchiveMedia
    {
        public TIngestStatus IngestStatus { get { return Get<TIngestStatus>(); } set { SetLocalValue(value); } }
    }
}
