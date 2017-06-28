using System;
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
