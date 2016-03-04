using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class ServerMedia : PersistentMedia, IServerMedia
    {
        public bool DoNotArchive { get { return Get<bool>(); } set { Set(value); } }
    }
}
