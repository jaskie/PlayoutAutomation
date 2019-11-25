using System.Collections.Generic;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public abstract class WatcherDirectory: MediaDirectoryBase, IWatcherDirectory
    {
        public abstract IReadOnlyCollection<IMedia> GetFiles();

        public void Refresh()
        {
            Invoke();
        }
    }
}
