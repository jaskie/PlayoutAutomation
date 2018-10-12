using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectory
    {
        #pragma warning disable CS0649



        #pragma warning restore
        

        public override IEnumerable<IMedia> GetFiles()
        {
            return Query<List<ArchiveMedia>>();
        }

        public IArchiveMedia Find(IMediaProperties media)
        {
            var ret =  Query<ArchiveMedia>(parameters: new object[] { media });
            return ret;
        }

        public IMediaManager MediaManager { get; set; }

        public void Search()
        {
            Invoke();
        }

        public List<IMedia> Search(TMediaCategory? category, string searchString)
        {
            throw new System.NotImplementedException();
        }

        public ulong IdArchive { get; set; }
    }
}
