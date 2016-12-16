using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IServerMedia: IPersistentMedia, IServerMediaProperties
    {
        bool IsArchived { get; }
    }

    public interface IServerMediaProperties: IPersistentMediaProperties
    {
        bool DoNotArchive { get; set; }
    }
}
