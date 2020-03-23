using System;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class MediaEventArgs : EventArgs
    {
        public MediaEventArgs(IMedia media)
        {
            Media = media;
        }
                
        public IMedia Media { get; }
    }


}
