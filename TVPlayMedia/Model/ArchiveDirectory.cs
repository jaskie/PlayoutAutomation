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
        public TMediaCategory? SearchMediaCategory { get { return Get<TMediaCategory?>(); } set { Set(value); } }
        
        public string SearchString { get { return Get<string>(); } set { Set(value); } }

        public void ArchiveRestore(IArchiveMedia media, IServerMedia mediaPGM, bool toTop)
        {
            Invoke(parameters: new object[] { media, mediaPGM, toTop });
        }

        public void ArchiveSave(IMedia media, TVideoFormat outputFormat, bool deleteAfterSuccess)
        {
            Invoke(parameters: new object[] { media, outputFormat, deleteAfterSuccess});
        }

        public IArchiveMedia Find(IMedia media)
        {
            var ret =  Query<ArchiveMedia>(parameters: new object[] { media });
            ret.Directory = this;
            return ret;
        }

        public IArchiveMedia GetArchiveMedia(IMedia media, bool searchExisting = true)
        {
            var ret = Query<ArchiveMedia>(parameters: new object[] { media, searchExisting });
            ret.Directory = this;
            return ret;
        }


        public void Search()
        {
            Invoke();
        }

        public override List<IMedia> Files
        {
            get
            {
                var list = Get<List<ArchiveMedia>>();
                list.ForEach(m => m.Directory = this);
                return list.Cast<IMedia>().ToList(); ;
            }
        }
    }
}
