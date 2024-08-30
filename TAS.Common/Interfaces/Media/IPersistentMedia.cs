using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces.Media
{
    public interface IPersistentMedia: IMedia, IPersistentMediaProperties
    {
        IDictionary<string, int> FieldLengths { get; }
        IMediaSegments GetMediaSegments();
        void Save();
    }

    public interface IPersistentMediaProperties: IMediaProperties
    {
        TMediaEmphasis MediaEmphasis { get; set; }
        string IdAux { get; set; }
        DateTime KillDate { get; set; }
        ulong IdProgramme { get; set; }
        bool IsProtected { get; set; }
    }
}
