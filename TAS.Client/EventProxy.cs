using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client
{
    public class EventProxy : IEventProperties
    {
        public decimal? AudioVolume { get; set; }
        public TimeSpan Duration { get; set; }
        public string EventName { get; set; }
        public TEventType EventType { get; set; }
        public EventGPI GPI { get; set; }
        public string IdAux { get; set; }
        public ulong IdProgramme { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHold { get; set; }
        public bool IsLoop { get; set; }
        public VideoLayer Layer { get; set; }
        public Guid MediaGuid { get; set; }
        public TPlayState PlayState { get; set; }
        public long Position { get; set; }
        public TimeSpan? RequestedStartTime { get; set; }
        public TimeSpan ScheduledDelay { get; set; }
        public TimeSpan ScheduledTc { get; set; }
        public DateTime ScheduledTime { get; set; }
        public TimeSpan StartTc { get; set; }
        public DateTime StartTime { get; set; }
        public TStartType StartType { get; set; }
        public TimeSpan TransitionTime { get; set; }
        public TTransitionType TransitionType { get; set; }

        public IEventProperties[] SubEvents { get; set; }

    }
}
