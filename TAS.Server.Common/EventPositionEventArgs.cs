using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Common
{
    public class EventPositionEventArgs: EventArgs
    {
        public EventPositionEventArgs(long position, TimeSpan timeToFinish)
        {
            Position = position;
            TimeToFinish = timeToFinish;
        }
        public long Position { get; }
        public TimeSpan TimeToFinish { get; }
    }
}
