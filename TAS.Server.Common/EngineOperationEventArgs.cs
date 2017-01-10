using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class EngineOperationEventArgs : EventArgs
    {
        public EngineOperationEventArgs(IEventClient AEvent, TEngineOperation AOperation)
        {
            Operation = AOperation;
            Event = AEvent;
        }
        public TEngineOperation Operation { get; private set; }
        public IEventClient Event { get; private set; }
    }
}
