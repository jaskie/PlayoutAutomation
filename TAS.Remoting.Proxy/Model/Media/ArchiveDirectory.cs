using System;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
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
