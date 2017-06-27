using System;

namespace TAS.Server.Common
{
    public class EngineTickEventArgs: EventArgs
    {
        public EngineTickEventArgs(DateTime currentTime, TimeSpan timeToAttention)
        {
            CurrentTime = currentTime;
            TimeToAttention = timeToAttention;
        }

        [Newtonsoft.Json.JsonProperty]
        public DateTime CurrentTime { get; private set; }

        [Newtonsoft.Json.JsonProperty]
        public TimeSpan TimeToAttention { get; private set; }
    }
}
