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
        public EngineOperationEventArgs(IEvent AEvent, TEngineOperation AOperation)
        {
            Operation = AOperation;
            Event = AEvent;
        }
        [Newtonsoft.Json.JsonProperty]
        public TEngineOperation Operation { get; private set; }
        [Newtonsoft.Json.JsonProperty(IsReference = true)]
        public IEvent Event { get; private set; }
    }
}
