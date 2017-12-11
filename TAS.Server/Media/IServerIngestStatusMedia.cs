using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    interface IServerIngestStatusMedia
    {
        TIngestStatus IngestStatus { get;  set; }
    }
}
