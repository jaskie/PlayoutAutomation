using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public abstract class WatcherDirectory: MediaDirectoryBase, IWatcherDirectory
    {
        public abstract IEnumerable<IMedia> GetFiles();

        public void Refresh()
        {
            Invoke();
        }
    }
}
