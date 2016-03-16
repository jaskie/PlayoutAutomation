using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class IEventEventArgs: EventArgs
    {
        public IEventEventArgs(IEvent ev)
        {
            Event = ev;
        }
        public IEvent Event { get; private set; }
    }
}
