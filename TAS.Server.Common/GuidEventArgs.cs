using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class GuidEventArgs : EventArgs
    {
        public GuidEventArgs(Guid guid)
        {
            Guid = guid;
        }
        public Guid Guid { get; private set; }
    }
}
