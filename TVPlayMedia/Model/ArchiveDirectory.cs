using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class ArchiveDirectory : MediaDirectory, IArchiveDirectory
    {
        public ulong idArchive { get { return Get<UInt64>(); } set { Set(value); } }

        public TMediaCategory? SearchMediaCategory { get { return Get<TMediaCategory?>(); } set { Set(value); } }
        
        public string SearchString { get { return Get<string>(); } set { Set(value); } }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerMedia destMedia, bool toTop)
        {
            Invoke(parameters: new object[] { srcMedia, destMedia, toTop });
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
            ret.Directory = this;
            return ret;
        }

        public IArchiveMedia GetArchiveMedia(IMediaProperties media, bool searchExisting = true)
        {
            var ret = Query<ArchiveMedia>(parameters: new object[] { media, searchExisting });
            ret.Directory = this;
            return ret;
        }
        
        public void Search()
        {
            Invoke();
        }
    }
}
