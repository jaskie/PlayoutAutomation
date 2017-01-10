using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class Event : ProxyBase, IEventClient
    {
        public decimal? AudioVolume { get { return Get<decimal?>(); }  set { SetField(value); } }

        public AutoStartFlags AutoStartFlags { get { return Get<AutoStartFlags>(); } set { SetField(value); } }

        public byte Crawl { get { return Get<byte>(); } set { SetField(value); } }

        public TimeSpan Duration { get { return Get<TimeSpan>(); } set { SetField(value); } }
        
        public DateTime EndTime { get { return Get<DateTime>(); } set { SetField(value); } }

        public IEngine Engine { get { return Get<Engine>(); } set { SetField(value); } }

        public string EventName { get { return Get<string>(); } set { SetField(value); } }

        public TEventType EventType { get { return Get<TEventType>(); } set { SetField(value); } }
        
        public string IdAux { get { return Get<string>(); } set { SetField(value); } }
        
        public ulong IdProgramme { get { return Get<ulong>(); } set { SetField(value); } }

        public ulong IdRundownEvent { get { return Get<ulong>(); } set { SetField(value); } }

        public bool IsCGEnabled { get { return Get<bool>(); } set { SetField(value); } }

        public bool IsDeleted { get { return Get<bool>(); } set { SetField(value); } }

        public bool IsEnabled { get { return Get<bool>(); } set { SetField(value); } }

        public bool IsForcedNext { get { return Get<bool>(); } set { SetField(value); } }

        public bool IsHold { get { return Get<bool>(); } set { SetField(value); } }

        public bool IsLoop { get { return Get<bool>(); } set { SetField(value); } }

        public bool IsModified { get { return Get<bool>(); } set { SetField(value); } }

        public VideoLayer Layer { get { return Get<VideoLayer>(); } set { SetField(value); } }

        public TimeSpan Length { get { return Get<TimeSpan>(); } set { SetField(value); } }

        public byte Logo { get { return Get<byte>(); } set { SetField(value); } }

        public IMedia Media { get { return Get<Media>(); } set { SetField(value); } }

        public Guid MediaGuid { get { return Get<Guid>(); } set { SetField(value); } }

        public IEventClient Next 
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan? Offset
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEventClient Parent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public byte Parental
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TPlayState PlayState
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IEventClient Prior
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan? RequestedStartTime
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan ScheduledDelay
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan ScheduledTc
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public DateTime ScheduledTime
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan StartTc
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public DateTime StartTime
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TStartType StartType
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IList<IEventClient> SubEvents
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int SubEventsCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TEasing TransitionEasing
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan TransitionPauseTime
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan TransitionTime
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TTransitionType TransitionType
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler Deleted;
        public event EventHandler<EventPositionEventArgs> PositionChanged;
        public event EventHandler Relocated;
        public event EventHandler Saved;
        public event EventHandler<CollectionOperationEventArgs<IEventClient>> SubEventChanged;

        public bool AllowDelete()
        {
            throw new NotImplementedException();
        }

        public MediaDeleteDenyReason CheckCanDeleteMedia(IServerMedia media)
        {
            throw new NotImplementedException();
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public IEvent CloneTree()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public TimeSpan? GetAttentionTime()
        {
            throw new NotImplementedException();
        }

        public decimal GetAudioVolume()
        {
            throw new NotImplementedException();
        }

        public void InsertAfter(IEventClient e)
        {
            throw new NotImplementedException();
        }

        public void InsertBefore(IEventClient e)
        {
            throw new NotImplementedException();
        }

        public void InsertUnder(IEventClient se)
        {
            throw new NotImplementedException();
        }

        public void MoveDown()
        {
            throw new NotImplementedException();
        }

        public void MoveUp()
        {
            throw new NotImplementedException();
        }

        public void Remove()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void UpdateScheduledTime(bool updateSuccessors)
        {
            throw new NotImplementedException();
        }
    }
}
