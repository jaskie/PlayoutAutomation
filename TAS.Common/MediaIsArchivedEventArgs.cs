using System;
using Newtonsoft.Json;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class MediaIsArchivedEventArgs : EventArgs
    {
        public MediaIsArchivedEventArgs(IMedia media, bool isArchived)
        {
            Media = media;
            IsArchived = isArchived;
        }

        [JsonProperty]
        public IMedia Media { get; private set; }

        [JsonProperty]
        public bool IsArchived { get; private set; }
    }


}
