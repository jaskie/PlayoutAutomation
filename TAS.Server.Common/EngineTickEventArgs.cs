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
            this.CurrentTime = currentTime;
            this.TimeToAttention = timeToAttention;
        }
        public DateTime CurrentTime { get; private set; }
        public TimeSpan TimeToAttention { get; private set; }
    }
}
