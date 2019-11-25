using System;
using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class EventEventArgs: EventArgs
    {
        public EventEventArgs(IEvent ev)
        {
            Event = ev;
        }
        [JsonProperty(IsReference = true, TypeNameHandling = TypeNameHandling.Objects)]
        public IEvent Event { get; private set; }
    }
}
