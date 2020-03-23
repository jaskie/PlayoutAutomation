using jNet.RPC;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public abstract class WatcherDirectory: MediaDirectoryBase, IWatcherDirectory
    {
        #pragma warning disable CS0649
        
        [DtoField(nameof(IWatcherDirectory.IsInitialized))]
        private bool _isInitialized;

        #pragma warning restore
        
        public void Refresh() => Invoke();

        public bool IsInitialized => _isInitialized;
        public virtual IReadOnlyCollection<IMedia> GetAllFiles() => Query<IReadOnlyCollection<IMedia>>();
    }
}
