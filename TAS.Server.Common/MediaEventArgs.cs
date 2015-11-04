using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class MediaEventArgs : EventArgs
    {
        public MediaEventArgs(IMedia media)
        {
            Media = media;
        }
        public IMedia Media { get; private set; }
    }
}
