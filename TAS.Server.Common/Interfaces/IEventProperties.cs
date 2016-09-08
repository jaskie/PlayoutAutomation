using System;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IEventProperties: ICGElementsState
    {
        decimal? AudioVolume { get; set; }
        TimeSpan Duration { get; set; }
        bool IsEnabled { get; set; }
        string EventName { get; set; }
        TEventType EventType { get; set; }
        bool IsHold { get; set; }
        bool IsLoop { get; set; }
        string IdAux { get; set; }
        ulong IdProgramme { get; set; }
        VideoLayer Layer { get; set; }
        Guid MediaGuid { get; set; }
        TimeSpan? RequestedStartTime { get; set; }
        TimeSpan ScheduledDelay { get; set; }
        TimeSpan ScheduledTc { get; set; }
        DateTime ScheduledTime { get; set; }
        TimeSpan StartTc { get; set; }
        DateTime StartTime { get; }
        TStartType StartType { get; set; }
        TimeSpan TransitionTime { get; set; }
        TimeSpan TransitionPauseTime { get; set; }
        TTransitionType TransitionType { get; set; }
        TEasing TransitionEasing { get; set; }
        AutoStartFlags AutoStartFlags { get; set; }
    }
}
