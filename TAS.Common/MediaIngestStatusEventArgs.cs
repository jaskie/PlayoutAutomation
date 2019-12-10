using System;
using Newtonsoft.Json;
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

        [JsonProperty]
        public IMedia Media { get; private set; }

        [JsonProperty]
        public TIngestStatus IngestStatus { get; private set; }
    }


}
