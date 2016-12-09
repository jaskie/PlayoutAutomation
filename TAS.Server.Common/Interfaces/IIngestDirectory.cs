using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IIngestDirectory: IMediaDirectory, IIngestDirectoryConfig
    {
        TDirectoryAccessType AccessType { get; }
        int XdcamClipCount { get; }
        string Filter { get; set; }
    }
}
