using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IServerMedia: IPersistentMedia
    {
        bool DoNotArchive { get; set; }
        bool IsArchived { get; }
    }
}
