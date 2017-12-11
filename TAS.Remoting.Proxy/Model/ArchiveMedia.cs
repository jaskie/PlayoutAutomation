using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class ArchiveMedia: PersistentMedia, IArchiveMedia
    {
        public TIngestStatus IngestStatus { get { return Get<TIngestStatus>(); } set { SetLocalValue(value); } }
    }
}
