using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public class ArchiveMedia: PersistentMedia, IArchiveMedia
    {
        #pragma warning disable CS0649 
    

        #pragma warning restore

        public TIngestStatus GetIngestStatus(IServerDirectory directory)
        {
            return Query<TIngestStatus>(parameters: new object[] {directory});
        }
    }
}
