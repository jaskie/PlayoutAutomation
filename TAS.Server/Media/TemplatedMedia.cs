using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class TemplatedMedia : ServerMedia, ITemplated
    {
        public TemplatedMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid, idPersistentMedia, null) { }
        public Dictionary<string, string> Fields
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
