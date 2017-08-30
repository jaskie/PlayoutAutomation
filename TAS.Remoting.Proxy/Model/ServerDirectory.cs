using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class ServerDirectory : MediaDirectory, IServerDirectory
    {
        public override IEnumerable<IMedia> GetFiles()
        {
            return Query<List<ServerMedia>>();
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }
    }
}
