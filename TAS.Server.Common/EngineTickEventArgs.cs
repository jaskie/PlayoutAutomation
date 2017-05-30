using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Common
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
