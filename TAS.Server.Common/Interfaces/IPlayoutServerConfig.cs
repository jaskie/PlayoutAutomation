using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces 
{
    public interface IPlayoutServerConfig : IPersistent
    {
        string ServerAddress { get; set; }
        string MediaFolder { get; set; }
        string AnimationFolder { get; set; }
        //IEnumerable<IPlayoutServerChannel> Channels { get; }
    }
}
