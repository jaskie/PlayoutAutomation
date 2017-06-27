using System;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Common
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
