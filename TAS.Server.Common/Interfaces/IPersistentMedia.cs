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
        TMediaEmphasis MediaEmphasis { get; set; }
        string IdAux { get; set; }
        ObservableSynchronizedCollection<IMediaSegment> MediaSegments { get; }
        IMediaSegment CreateSegment();
        DateTime KillDate { get; set; }
        UInt64 IdProgramme { get; set; }
        UInt64 IdPersistentMedia { get; set; }
        bool Save();
    }
}
