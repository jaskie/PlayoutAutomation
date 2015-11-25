using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Common
{
    public class MediaDtoEventArgs : DtoEventArgs
    {
        public MediaDtoEventArgs(Guid dtoGuid, Guid mediaGuid): base(dtoGuid)
        {
            MediaGuid = mediaGuid;
        }
        public Guid MediaGuid { get; private set; }
    }
}
