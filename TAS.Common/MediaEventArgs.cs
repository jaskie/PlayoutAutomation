using System;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class MediaEventArgs : EventArgs
    {
        public MediaEventArgs(IMedia media)
        {
            Media = media;
        }
        [Newtonsoft.Json.JsonProperty]
        public IMedia Media { get; private set; }
    }


}
