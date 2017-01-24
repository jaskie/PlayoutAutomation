using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IPersistentMedia: IMedia, IPersistentMediaProperties
    {
        IMediaSegments MediaSegments { get; }
        bool IsModified { get; set; }
        bool Save();
    }

    public interface IPersistentMediaProperties: IMediaProperties
    {
        TMediaEmphasis MediaEmphasis { get; set; }
        string IdAux { get; set; }
        DateTime KillDate { get; set; }
        UInt64 IdProgramme { get; set; }
        UInt64 IdPersistentMedia { get; set; }
        bool Protected { get; set; }
    }
}
