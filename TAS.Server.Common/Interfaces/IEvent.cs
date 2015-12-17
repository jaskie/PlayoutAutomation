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
        IServerMedia ServerMediaPGM { get; }
        IServerMedia ServerMediaPRV { get; }
        IMedia Media { get; set; }
        IEngine Engine { get; }
        long SeekPGM { get; }
        bool Finished { get; }
        TimeSpan Length { get; }
        DateTime EndTime { get; }

        IEvent Next { get; }
        IEvent Prior { get; }
        IEvent Parent { get; }
        IEvent VisualParent { get; }
        List<IEvent> VisualRootTrack { get; }

        SynchronizedCollection<IEvent> SubEvents { get; }
        void InsertAfter(IEvent e);
        void InsertBefore(IEvent e);
        void InsertUnder(IEvent se);
        void MoveUp();
        void MoveDown();
        void Remove();
        void Save();
        void Delete();
        IEvent Clone();

        bool Modified { get; }
        bool IsDeleted { get; }
        MediaDeleteDenyReason CheckCanDeleteMedia(IServerMedia media);
        IEvent GetSuccessor();
        bool IsContainedIn(IEvent parent);
        bool IsBefore(IEvent aEvent);
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
