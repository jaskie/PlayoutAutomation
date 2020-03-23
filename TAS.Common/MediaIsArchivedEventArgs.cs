using System;
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

        public IMedia Media { get; }
                
        public bool IsArchived { get; }
    }


}
