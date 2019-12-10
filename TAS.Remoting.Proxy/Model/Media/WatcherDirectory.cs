using System.Collections.Generic;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public abstract class WatcherDirectory: MediaDirectoryBase, IWatcherDirectory
    {
        public void Refresh()
        {
            Invoke();
        }

        public virtual IReadOnlyCollection<IMedia> GetAllFiles()
        {
            return Query<IReadOnlyCollection<IMedia>>();
        }
    }
}
