using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class ServerDirectory : MediaDirectory, IServerDirectory
    {
        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }
    }
}
