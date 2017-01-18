using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    [DebuggerDisplay("{EventName}")]
    public class Event : ProxyBase, IEvent
    {
        public decimal? AudioVolume { get { return Get<decimal?>(); }  set { Set(value); } }

        public AutoStartFlags AutoStartFlags { get { return Get<AutoStartFlags>(); } set { Set(value); } }

        public byte Crawl { get { return Get<byte>(); } set { Set(value); } }

        public TimeSpan Duration { get { return Get<TimeSpan>(); } set { Set(value); } }
        
        public DateTime EndTime { get { return Get<DateTime>(); } set { Set(value); } }

        public IEngine Engine { get { return Get<Engine>(); } set { Set(value); } }

        public string EventName { get { return Get<string>(); } set { Set(value); } }

        public TEventType EventType { get { return Get<TEventType>(); } set { SetLocalValue(value); } }
        
        public string IdAux { get { return Get<string>(); } set { Set(value); } }
        
        public ulong IdProgramme { get { return Get<ulong>(); } set { Set(value); } }

        public bool IsCGEnabled { get { return Get<bool>(); } set { Set(value); } }

        public bool IsDeleted { get { return Get<bool>(); } set { Set(value); } }

        public bool IsEnabled { get { return Get<bool>(); } set { Set(value); } }

        public bool IsForcedNext { get { return Get<bool>(); } set { Set(value); } }

        public bool IsHold { get { return Get<bool>(); } set { Set(value); } }

        public bool IsLoop { get { return Get<bool>(); } set { Set(value); } }

        public bool IsModified { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public VideoLayer Layer { get { return Get<VideoLayer>(); } set { Set(value); } }

        public TimeSpan Length { get { return Get<TimeSpan>(); } set { SetLocalValue(value); } }

        public byte Logo { get { return Get<byte>(); } set { Set(value); } }

        [JsonProperty(nameof(IEvent.Media))]
        private Media _media { get { return Get<Media>(); } set { Set(value); } }
        [JsonIgnore]
        public IMedia Media { get { return _media; } set { _media = value as Media; } }

        public Guid MediaGuid { get { return Get<Guid>(); } set { Set(value); } }

        public IEvent Next { get { return Get<Event>(); } set { Set(value); } }
        
        public TimeSpan? Offset { get { return Get<TimeSpan?>(); } set { Set(value); } }

        public IEvent Parent { get { return Get<Event>(); } set { Set(value); } }

        public byte Parental { get { return Get<byte>(); } set { Set(value); } }

        public TPlayState PlayState { get { return Get<TPlayState>(); } set { Set(value); } }

        public IEvent Prior { get { return Get<Event>(); } set { Set(value); } }

        public TimeSpan? RequestedStartTime { get { return Get<TimeSpan?>(); }  set { Set(value); } }

        public TimeSpan ScheduledDelay { get { return Get<TimeSpan>(); } set { Set(value); } }

        public TimeSpan ScheduledTc { get { return Get<TimeSpan>(); } set { Set(value); } }

        public DateTime ScheduledTime { get { return Get<DateTime>(); } set { Set(value); } }

        public TimeSpan StartTc { get { return Get<TimeSpan>(); } set { Set(value); } }

        public DateTime StartTime { get { return Get<DateTime>(); } set { Set(value); } }

        public TStartType StartType { get { return Get<TStartType>(); } set { Set(value); } }

        public IList<IEvent> SubEvents
        {
            get
            {
                var result = Get<List<IEvent>>();
                if (result == null)
                    result = new List<IEvent>();
                return result;
            }
            set { SetLocalValue(value); }
        }

        public int SubEventsCount { get { return Get<int>(); } set { SetLocalValue(value); } }

        public TEasing TransitionEasing { get { return Get<TEasing>(); } set { Set(value); } }

        public TimeSpan TransitionPauseTime { get { return Get<TimeSpan>(); } set { Set(value); } }

        public TimeSpan TransitionTime { get { return Get<TimeSpan>(); } set { Set(value); } }

        public TTransitionType TransitionType { get { return Get<TTransitionType>(); } set { Set(value); } }

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

        private void _subeventChanged(CollectionOperationEventArgs<IEvent> e)
        {
            object subEvents;
            if (Properties.TryGetValue(nameof(SubEvents), out subEvents))
            {
                var list = subEvents as IList<IEvent>;
                if (list != null)
                {
                    if (e.Operation == TCollectionOperation.Insert)
                        list.Add(e.Item);
                    else
                        list.Remove(e.Item);
                    SubEventsCount = list.Count;
                    NotifyPropertyChanged(nameof(SubEventsCount));
                }
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
                    var ea = ConvertEventArgs<CollectionOperationEventArgs<IEvent>>(e);
                    _subeventChanged(ea);
                    _subEventChanged.Invoke(this, ea);
                    return;
            }
        }

        #endregion //Event handlers

        public bool AllowDelete() { return Query<bool>(); }

        public void Delete() { Invoke(); }
        public void InsertAfter(IEvent e) { Invoke(parameters: new[] { e }); }
        public void InsertBefore(IEvent e) { Invoke(parameters: new[] { e }); }
        public void InsertUnder(IEvent se, bool fromEnd) { Invoke(parameters: new object[] { se, fromEnd }); }
        public void MoveDown() { Invoke(); }
        public void MoveUp() { Invoke(); }
        public void Remove() { Invoke(); }
        public void Save()
        {
            Invoke();
        }
    }
}
