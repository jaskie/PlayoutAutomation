using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IEvent: IEventProperties, INotifyPropertyChanged
    {
        UInt64 IdRundownEvent { get; set; }
        UInt64 IdEventBinding { get; }
        IMedia Media { get; set; }
        IEngine Engine { get; }
        long MediaSeek { get; }
        bool IsFinished { get; }
        TimeSpan Length { get; }
        DateTime EndTime { get; }
        TimeSpan? Offset { get; }

        IEvent Next { get; }
        IEvent Prior { get; }
        IEvent Parent { get; }
        IEvent VisualParent { get; }

        SynchronizedCollection<IEvent> SubEvents { get; }
        void InsertAfter(IEvent e);
        void InsertBefore(IEvent e);
        void InsertUnder(IEvent se);
        void MoveUp();
        void MoveDown();
        void Remove();
        void Save();
        void Delete();
        bool AllowDelete();
        IEvent Clone();

        bool Modified { get; }
        bool IsDeleted { get; }
        MediaDeleteDenyReason CheckCanDeleteMedia(IServerMedia media);
        IEvent GetSuccessor();
        IEnumerable<IEvent> GetVisualRootTrack();
        bool IsContainedIn(IEvent parent);
        decimal GetAudioVolume();
        TimeSpan? GetAttentionTime();
        void UpdateScheduledTime(bool updateSuccessors);

        event EventHandler Saved;
        event EventHandler Deleted;
        event EventHandler Relocated;
        event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;
        event EventHandler<EventPositionEventArgs> PositionChanged;
    }
}
