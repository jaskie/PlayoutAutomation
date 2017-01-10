using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IEvent: IEventClient, IEventDatabase
    {
    }

    public interface IEventClient: IEventProperties, INotifyPropertyChanged, ICloneable
    {
        TPlayState PlayState { get; set; }
        long Position { get; set; }
        IMedia Media { get; set; }
        IEngine Engine { get; }
        TimeSpan Length { get; }
        DateTime EndTime { get; }
        TimeSpan? Offset { get; }

        IEventClient Next { get; }
        IEventClient Prior { get; }
        IEventClient Parent { get; }

        IList<IEventClient> SubEvents { get; }
        int SubEventsCount { get; }
        void InsertAfter(IEventClient e);
        void InsertBefore(IEventClient e);
        void InsertUnder(IEventClient se);
        void MoveUp();
        void MoveDown();
        void Remove();
        void Save();
        void Delete();
        bool AllowDelete();
        bool IsModified { get; set; }
        bool IsDeleted { get; }
        MediaDeleteDenyReason CheckCanDeleteMedia(IServerMedia media);
        bool IsForcedNext { get; }
        decimal GetAudioVolume();
        TimeSpan? GetAttentionTime();
        void UpdateScheduledTime(bool updateSuccessors);

        event EventHandler Saved;
        event EventHandler Deleted;
        event EventHandler Relocated;
        event EventHandler<CollectionOperationEventArgs<IEventClient>> SubEventChanged;
        event EventHandler<EventPositionEventArgs> PositionChanged;
    }

    public interface IEventDatabase: IEventProperties
    {
        ulong IdRundownEvent { get; set; }
        ulong IdEventBinding { get; }
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
        string IdAux { get; set; }
        ulong IdProgramme { get; set; }
        VideoLayer Layer { get; set; }
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
        Guid MediaGuid { get; set; }
    }

}
