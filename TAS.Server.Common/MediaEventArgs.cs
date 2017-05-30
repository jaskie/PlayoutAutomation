using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Common
{
    public class MediaEventArgs : EventArgs
    {
        public MediaEventArgs(IMedia media)
        {
            Media = media;
        }
        [Newtonsoft.Json.JsonProperty]
        public IMedia Media { get; }
    }


}
