using System;

namespace TAS.Common
{
    public class EngineTickEventArgs: EventArgs
    {
        public EngineTickEventArgs(DateTime currentTime, TimeSpan timeToAttention)
        {
            CurrentTime = currentTime;
            TimeToAttention = timeToAttention;
        }

        public DateTime CurrentTime { get; }

        public TimeSpan TimeToAttention { get; }
    }
}
