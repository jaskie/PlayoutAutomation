using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class ServerDirectory : WatcherDirectory, IServerDirectory
    {
        public override IReadOnlyCollection<IMedia> GetFiles()
        {
            return Query<ReadOnlyCollection<IMedia>>();
        }
    }
}
