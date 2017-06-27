using System;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Common
{
    public class EngineOperationEventArgs : EventArgs
    {
        public EngineOperationEventArgs(IEvent aEvent, TEngineOperation aOperation)
        {
            Operation = aOperation;
            Event = aEvent;
        }
        [Newtonsoft.Json.JsonProperty]
        public TEngineOperation Operation { get; private set; }
        [Newtonsoft.Json.JsonProperty(IsReference = true)]
        public IEvent Event { get; private set; }
    }
}
