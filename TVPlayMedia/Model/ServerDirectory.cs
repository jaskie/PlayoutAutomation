using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class ServerDirectory : MediaDirectory, IServerDirectory
    {
        public IPlayoutServer Server
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IServerMedia GetServerMedia(IMedia media, bool searchExisting = true)
        {
            return Query<ServerMedia>(parameters: new object[] { media, searchExisting });
        }

        public override IEnumerable<IMedia> GetFiles()
        {
            var list = Query<List<ServerMedia>>();
            list.ForEach(m => m.Directory = this);
            return list.Cast<IMedia>().ToList(); ;
        }

        public override IMedia FindMediaByDto(Guid dtoGuid)
        {
            ServerMedia result = Query<ServerMedia>(parameters: new[] { dtoGuid });
            result.Directory = this;
            return result;
        }

    }
}
