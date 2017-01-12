using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class ArchiveDirectory : MediaDirectory, IArchiveDirectory
    {
        public ulong idArchive { get { return Get<UInt64>(); } set { Set(value); } }

        public TMediaCategory? SearchMediaCategory { get { return Get<TMediaCategory?>(); } set { Set(value); } }
        
        public string SearchString { get { return Get<string>(); } set { Set(value); } }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerDirectory destDirectory, bool toTop)
        {
            Invoke(parameters: new object[] { srcMedia, destDirectory, toTop });
        }
        
        public void ArchiveSave(IServerMedia media, bool deleteAfterSuccess)
        {
            Invoke(parameters: new object[] { media, deleteAfterSuccess});
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            return Query<IMedia>(parameters: new [] { mediaProperties });
        }

        public IArchiveMedia Find(IMediaProperties media)
        {
            var ret =  Query<ArchiveMedia>(parameters: new object[] { media });
            return ret;
        }

        public void Search()
        {
            Invoke();
        }
    }
}
