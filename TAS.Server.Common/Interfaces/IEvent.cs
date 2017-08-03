using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Server.Common.Interfaces
{
    public interface IEventPesistent: IEvent
    {
        ulong IdEventBinding { get; }
    }

    public interface IEvent: IEventProperties, IAclObject, IPersistent, INotifyPropertyChanged
    {
        TPlayState PlayState { get; set; }
        IMedia Media { get; set; }
        IEngine Engine { get; }
        TimeSpan Length { get; }
        DateTime EndTime { get; }
        TimeSpan? Offset { get; }
        
        IEvent Next { get; }
        IEvent Prior { get; }
        IEvent Parent { get; }

        List<IEvent> SubEvents { get; }

        int SubEventsCount { get; }
        void InsertAfter(IEvent e);
        void InsertBefore(IEvent e);
        void InsertUnder(IEvent se, bool fromEnd);
        void MoveUp();
        void MoveDown();
        void Remove();
        bool AllowDelete();
        bool IsModified { get; set; }
        bool IsDeleted { get; }
        bool IsForcedNext { get; }

        event EventHandler Saved;
        event EventHandler Deleted;
        event EventHandler Relocated;
        event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;
        event EventHandler<EventPositionEventArgs> PositionChanged;
    }

    public interface IEventProperties : ICGElementsState
    {
        decimal? AudioVolume { get; set; }
        TimeSpan Duration { get; set; }
        bool IsEnabled { get; set; }
        string EventName { get; set; }
        TEventType EventType { get; set; }
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
