using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class ServerDirectory : WatcherDirectory, IServerDirectory
    {
        public override async Task<IEnumerable<IMedia>> GetFiles()
        {
            return await Task.Run(() => Query<ReadOnlyCollection<IMedia>>());
        }
    }
}
