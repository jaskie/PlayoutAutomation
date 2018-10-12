using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class ServerDirectory : MediaDirectoryBase, IServerDirectory
    {
        public override IEnumerable<IMedia> GetFiles()
        {
            return Query<ReadOnlyCollection<IMedia>>();
        }
    }
}
