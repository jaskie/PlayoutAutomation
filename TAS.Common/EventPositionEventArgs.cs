using System;

namespace TAS.Common
{
    public class EventPositionEventArgs: EventArgs
    {
        public EventPositionEventArgs(long position, TimeSpan timeToFinish)
        {
            Position = position;
            TimeToFinish = timeToFinish;
        }
        [Newtonsoft.Json.JsonProperty]
        public long Position { get; private set; }
        [Newtonsoft.Json.JsonProperty]
        public TimeSpan TimeToFinish { get; private set; }
    }
}
