using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectory
    {

        public IArchiveMedia Find(IMediaProperties media)
        {
            var ret =  Query<ArchiveMedia>(parameters: new object[] { media });
            return ret;
        }

        public IMediaManager MediaManager { get; set; }

        public List<IMedia> Search(TMediaCategory? category, string searchString)
        {
            return Query<List<IMedia>>(parameters: new object[] {category, searchString});
        }
        
    }
}
