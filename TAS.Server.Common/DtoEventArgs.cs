using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class DtoEventArgs : EventArgs
    {
        public DtoEventArgs(Guid dtoGuid)
        {
            DtoGuid = dtoGuid;
        }
        public Guid DtoGuid { get; private set; }
    }
}
