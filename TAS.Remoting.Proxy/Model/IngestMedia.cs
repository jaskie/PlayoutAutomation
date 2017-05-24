using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class IngestMedia: Media, IIngestMedia
    {
        public TIngestStatus IngestStatus { get { return Get<TIngestStatus>(); } set { SetLocalValue(value); } }
    }
}
