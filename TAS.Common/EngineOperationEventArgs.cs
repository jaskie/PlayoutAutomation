using System;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class EngineOperationEventArgs : EventArgs
    {
        public EngineOperationEventArgs(IEvent @event, TEngineOperation operation)
        {
            Operation = operation;
            Event = @event;
        }
        
        public TEngineOperation Operation { get; }
        
        public IEvent Event { get; }
    }
}
