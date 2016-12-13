using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class ServerDirectory : MediaDirectory, IServerDirectory
    {
        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }
    }
}
