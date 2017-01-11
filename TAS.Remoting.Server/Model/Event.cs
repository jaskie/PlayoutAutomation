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
    public class Event : ProxyBase, IEvent
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

        public IEvent Next { get { return Get<Event>(); } set { SetField(value); } }
        
        public TimeSpan? Offset { get { return Get<TimeSpan?>(); } set { SetField(value); } }

        public IEvent Parent { get { return Get<Event>(); } set { SetField(value); } }

        public byte Parental { get { return Get<byte>(); } set { SetField(value); } }

        public TPlayState PlayState { get { return Get<TPlayState>(); } set { SetField(value); } }

        public IEvent Prior { get { return Get<Event>(); } set { SetField(value); } }

        public TimeSpan? RequestedStartTime { get { return Get<TimeSpan?>(); }  set { SetField(value); } }

        public TimeSpan ScheduledDelay { get { return Get<TimeSpan>(); } set { SetField(value); } }

        public TimeSpan ScheduledTc { get { return Get<TimeSpan>(); } set { SetField(value); } }

        public DateTime ScheduledTime { get { return Get<DateTime>(); } set { SetField(value); } }

        public TimeSpan StartTc { get { return Get<TimeSpan>(); } set { SetField(value); } }

        public DateTime StartTime { get { return Get<DateTime>(); } set { SetField(value); } }

        public TStartType StartType { get { return Get<TStartType>(); } set { SetField(value); } }

        public IList<IEvent> SubEvents { get { return Get<List<IEvent>>(); } set { SetField(value); } }

        public int SubEventsCount { get { return Get<int>(); } set { SetField(value); } }

        public TEasing TransitionEasing { get { return Get<TEasing>(); } set { SetField(value); } }

        public TimeSpan TransitionPauseTime { get { return Get<TimeSpan>(); } set { SetField(value); } }

        public TimeSpan TransitionTime { get { return Get<TimeSpan>(); } set { SetField(value); } }

        public TTransitionType TransitionType { get { return Get<TTransitionType>(); } set { SetField(value); } }

        #region Event handlers
        event EventHandler _deleted;
        public event EventHandler Deleted
        {
            add
            {
                EventAdd(_deleted);
                _deleted += value;
            }
            remove
            {
                _deleted -= value;
                EventRemove(_deleted);
            }
        }
        event EventHandler<EventPositionEventArgs> _positionChanged;
        public event EventHandler<EventPositionEventArgs> PositionChanged
        {
            add
            {
                EventAdd(_positionChanged);
                _positionChanged += value;
            }
            remove
            {
                _positionChanged -= value;
                EventRemove(_positionChanged);
            }
        }
        event EventHandler _relocated;
        public event EventHandler Relocated
        {
            add
            {
                EventAdd(_relocated);
                _relocated += value;
            }
            remove
            {
                _relocated -= value;
                EventRemove(_relocated);
            }
        }
        event EventHandler _saved;
        public event EventHandler Saved
        {
            add
            {
                EventAdd(_saved);
                _saved += value;
            }
            remove
            {
                _saved -= value;
                EventRemove(_saved);
            }
        }
        event EventHandler<CollectionOperationEventArgs<IEvent>> _subEventChanged;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged
        {
            add
            {
                EventAdd(_subEventChanged);
                _subEventChanged += value;
            }
            remove
            {
                _subEventChanged -= value;
                EventRemove(_subEventChanged);
            }
        }

        protected override void OnEventNotification(WebSocketMessage e)
        {
            switch (e.MemberName)
            {
                case nameof(IEvent.Deleted):
                    _deleted.Invoke(this, ConvertEventArgs<EventArgs>(e));
                    break;
                case nameof(IEvent.PositionChanged):
                    _positionChanged.Invoke(this, ConvertEventArgs<EventPositionEventArgs>(e));
                    break;
                case nameof(IEvent.Relocated):
                    _relocated.Invoke(this, ConvertEventArgs<EventArgs>(e));
                    return;
                case nameof(IEvent.Saved):
                    _saved.Invoke(this, ConvertEventArgs<EventArgs>(e));
                    return;
                case nameof(IEvent.SubEventChanged):
                    _subEventChanged.Invoke(this, ConvertEventArgs<CollectionOperationEventArgs<IEvent>>(e));
                    return;
            }
        }

        #endregion //Event handlers

        public bool AllowDelete() { return Query<bool>(); }

        public MediaDeleteDenyReason CheckCanDeleteMedia(IServerMedia media)
        {
            throw new NotImplementedException();
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public IEventPesistent CloneTree()
        {
            throw new NotImplementedException();
        }

        public void Delete() { Invoke(); }
        public void InsertAfter(IEvent e) { Invoke(parameters: new[] { e }); }
        public void InsertBefore(IEvent e) { Invoke(parameters: new[] { e }); }
        public void InsertUnder(IEvent se) { Invoke(parameters: new[] { se }); }
        public void MoveDown() { Invoke(); }
        public void MoveUp() { Invoke(); }
        public void Remove() { Invoke(); }
        public void Save() { Invoke(); }
    }
}
