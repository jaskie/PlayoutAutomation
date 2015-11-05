using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IPersistentMedia: IMedia
    {
        IMedia OriginalMedia { get; set; }
        TMediaEmphasis MediaEmphasis { get; set; }
        ObservableSynchronizedCollection<IMediaSegment> MediaSegments { get; }
        IMediaSegment CreateSegment();
        bool Save();
    }
}
