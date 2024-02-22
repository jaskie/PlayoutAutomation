using System;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class EventEventArgs: EventArgs
    {
        public EventEventArgs(IEvent @event)
        {
            Event = @event;
        }

        public IEvent Event { get; }
    }
}
