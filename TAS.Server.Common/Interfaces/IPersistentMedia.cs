using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IPersistentMedia: IMedia
    {
        IMedia OriginalMedia { get; set; }
        ObservableSynchronizedCollection<IMediaSegment> MediaSegments { get; }
        bool Save();
    }
}
