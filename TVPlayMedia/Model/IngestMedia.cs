using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class IngestMedia: Media, IIngestMedia
    {
        public override IMediaDirectory Directory { get { return Get<IIngestDirectory>(); } set { SetField(value); } }
    }
}
