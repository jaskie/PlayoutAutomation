using System;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class EventEventArgs: EventArgs
    {
        public EventEventArgs(IEvent ev)
        {
            Event = ev;
        }
        [Newtonsoft.Json.JsonProperty(IsReference = true)]
        public IEvent Event { get; private set; }
    }
}
