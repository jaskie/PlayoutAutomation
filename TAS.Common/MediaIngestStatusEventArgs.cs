using System;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class MediaIngestStatusEventArgs : EventArgs
    {
        public MediaIngestStatusEventArgs(IMedia media, TIngestStatus ingestStatus)
        {
            Media = media;
            IngestStatus = ingestStatus;
        }

        public IMedia Media { get; }

        public TIngestStatus IngestStatus { get; }
    }

}
