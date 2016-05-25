using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public TimeSpan? RequestedStartTime { get; set; }
        public TimeSpan ScheduledDelay { get; set; }
        public TimeSpan ScheduledTc { get; set; }
        public DateTime ScheduledTime { get; set; }
        public TimeSpan StartTc { get; set; }
        public DateTime StartTime { get; set; }
        public TStartType StartType { get; set; }
        public TimeSpan TransitionTime { get; set; }
        public TTransitionType TransitionType { get; set; }
        public EventProxy[] SubEvents { get; set; }
        public static EventProxy FromEvent(IEvent source){
            return new EventProxy()
            {
                AudioVolume = source.AudioVolume,
                Duration = source.Duration,
                EventName = source.EventName,
                EventType = source.EventType,
                GPI = source.GPI,
                IdAux = source.IdAux,
                IdProgramme = source.IdProgramme,
                IsEnabled = source.IsEnabled,
                IsHold = source.IsHold,
                IsLoop = source.IsLoop,
                Layer = source.Layer,
                MediaGuid = source.MediaGuid,
                RequestedStartTime = source.RequestedStartTime,
                ScheduledDelay = source.ScheduledDelay,
                ScheduledTc = source.ScheduledTc,
                ScheduledTime = source.ScheduledTime,
                StartTc = source.StartTc,
                StartTime = source.StartTime,
                StartType = source.StartType,
                TransitionTime = source.TransitionTime,
                TransitionType = source.TransitionType,
                SubEvents = source.AllSubEvents().Select(e => FromEvent(e)).ToArray(),                
            };
        }
        public override string ToString()
        {
            return string.Format("{0}:{1}", EventName, SubEvents.Length);
        }
    }

    static class IEventExtensions
    {
        public static IEnumerable<IEvent> AllSubEvents(this IEvent e)
        {
            IEnumerable<IEvent> sel = e.SubEvents;
            foreach (IEvent selItem in sel)
            {
                yield return selItem;
                IEvent nextItem = selItem;
                while ((nextItem = nextItem.Next)!= null)
                {
                    yield return nextItem;
                }
            }
        }
    }
}
