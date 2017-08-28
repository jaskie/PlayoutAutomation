using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class ServerMedia : PersistentMedia, IServerMedia
    {
        public bool DoNotArchive { get { return Get<bool>(); } set { Set(value); } }
        public bool IsArchived { get { return Get<bool>(); } set { Set(value); } }
    }
}
