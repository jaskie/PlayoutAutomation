using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    public class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectory
    {

        public IArchiveMedia Find(Guid mediaGuid)
        {
            var ret =  Query<ArchiveMedia>(parameters: new object[] { mediaGuid });
            return ret;
        }

        public IMediaManager MediaManager { get; set; }
        
    }
}
