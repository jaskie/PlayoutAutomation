using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces.Media
{
    public interface IPersistentMedia: IMedia, IPersistentMediaProperties
    {
        bool IsModified { get; set; }
        IDictionary<string, int> FieldLengths { get; }
        IMediaSegments GetMediaSegments();
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
