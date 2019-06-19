using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.Security;

namespace TAS.Common.Interfaces
{
    public interface IEventPersistent: IEvent
    {
        ulong IdEventBinding { get; }
        bool IsModified { get; set; }
    }

    public interface IEvent: IEventProperties, IAclObject, IPersistent, INotifyPropertyChanged
    {
        TPlayState PlayState { get; }
        IMedia Media { get; set; }
        IEngine Engine { get; }
        DateTime EndTime { get; }
        TimeSpan? Offset { get; }
        
        IEvent Next { get; }
        IEvent Prior { get; }
        IEvent Parent { get; }

        IEnumerable<IEvent> SubEvents { get; }

        int SubEventsCount { get; }
        bool InsertAfter(IEvent e);
        bool InsertBefore(IEvent e);
        bool InsertUnder(IEvent se, bool fromEnd);
        bool MoveUp();
        bool MoveDown();
        void Remove();
        bool AllowDelete();
        bool IsDeleted { get; }
        bool IsForcedNext { get; }
        ulong CurrentUserRights { get; }
        bool HaveRight(EventRight right);
        IEvent GetSuccessor();

        event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;
        event EventHandler<EventPositionEventArgs> PositionChanged;
    }

    public interface IEventProperties : ICGElementsState
    {
        double? AudioVolume { get; set; }
        TimeSpan Duration { get; set; }
        bool IsEnabled { get; set; }
        string EventName { get; set; }
        TEventType EventType { get; }
        bool IsHold { get; set; }
        bool IsLoop { get; set; }
        string IdAux { get; set; } // auxiliary Id for external systems
        ulong IdProgramme { get; set; }
        VideoLayer Layer { get; set; }
        TimeSpan? RequestedStartTime { get; set; } // informational only: when it should run according to schedule. Usefull when adding or removing previous events
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
        Guid MediaGuid { get; set; }
    }

}
