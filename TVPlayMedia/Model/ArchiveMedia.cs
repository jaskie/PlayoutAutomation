using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class ArchiveMedia: PersistentMedia, IArchiveMedia
    {
        public override IMediaDirectory Directory { get { return Get<IArchiveDirectory>(); } set { SetField(value); } }
        public TIngestStatus IngestState { get { return Get<TIngestStatus>(); } set { SetField(value); } }
    }
}
