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
        
        public long Position { get; }
        
        public TimeSpan TimeToFinish { get; }
    }
}
