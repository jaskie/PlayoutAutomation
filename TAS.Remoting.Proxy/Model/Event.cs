using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Model.Security;

namespace TAS.Remoting.Model
{
    [DebuggerDisplay("{" + nameof(EventName) + "}")]
    public class Event : ProxyBase, IEvent
    {
        #pragma warning disable CS0649 

        [JsonProperty(nameof(IEvent.Id))]
        private ulong _id;

        [JsonProperty(nameof(IEvent.AudioVolume))]
        private double? _audioVolume;

        [JsonProperty(nameof(IEvent.AutoStartFlags))]
        private AutoStartFlags _autoStartFlags;

        [JsonProperty(nameof(IEvent.Crawl))]
        private byte _crawl;

        [JsonProperty(nameof(IEvent.Duration))]
        private TimeSpan _duration;

        [JsonProperty(nameof(IEvent.EndTime))]
        private DateTime _endTime;

        [JsonProperty(nameof(IEvent.Engine))]
        private IEngine _engine;

        [JsonProperty(nameof(IEvent.EventName))]
        private string _eventName;

        [JsonProperty(nameof(IEvent.EventType))]
        private TEventType _eventType;

        [JsonProperty(nameof(IEvent.IdAux))]
        private string _idAux;

        [JsonProperty(nameof(IEvent.IdProgramme))]
        private ulong _idProgramme;

        [JsonProperty(nameof(IEvent.IsCGEnabled))]
        private bool _isCGEnabled;

        [JsonProperty(nameof(IEvent.IsDeleted))]
        private bool _isDeleted;

        [JsonProperty(nameof(IEvent.IsEnabled))]
        private bool _isEnabled;

        [JsonProperty(nameof(IEvent.IsForcedNext))]
        private bool _isForcedNext;

        [JsonProperty(nameof(IEvent.IsHold))]
        private bool _isHold;

        [JsonProperty(nameof(IEvent.IsLoop))]
        private bool _isLoop;

        [JsonProperty(nameof(IEvent.IsModified))]
        private bool _isModified;

        [JsonProperty(nameof(IEvent.Layer))]
        private VideoLayer _layer;

        [JsonProperty(nameof(IEvent.Length))]
        private TimeSpan _length;

        [JsonProperty(nameof(IEvent.Logo))]
        private byte _logo;

        [JsonProperty(nameof(IEvent.Media))]
        private ServerMedia _media;

        [JsonProperty(nameof(IEvent.MediaGuid))]
        private Guid _mediaGuid;

        [JsonProperty(nameof(IEvent.Offset))]
        private TimeSpan? _offset;

        [JsonProperty(nameof(IEvent.Parental))]
        private byte _parental;

        [JsonProperty(nameof(IEvent.PlayState))]
        private TPlayState _playState;

        [JsonProperty(nameof(IEvent.RequestedStartTime))]
        private TimeSpan? _requestedStartTime;

        [JsonProperty(nameof(IEvent.ScheduledDelay))]
        private TimeSpan _scheduledDelay;

        [JsonProperty(nameof(IEvent.ScheduledTc))]
        private TimeSpan _scheduledTc;

        [JsonProperty(nameof(IEvent.ScheduledTime))]
        private DateTime _scheduledTime;

        [JsonProperty(nameof(IEvent.StartTc))]
        private TimeSpan _startTc;

        [JsonProperty(nameof(IEvent.StartTime))]
        private DateTime _startTime;

        [JsonProperty(nameof(IEvent.StartType))]
        private TStartType _startType;

        [JsonProperty(nameof(IEvent.SubEventsCount))]
        private int _subEventsCount;

        [JsonProperty(nameof(IEvent.TransitionEasing))]
        private TEasing _transitionEasing;

        [JsonProperty(nameof(IEvent.TransitionPauseTime))]
        private TimeSpan _transitionPauseTime;

        [JsonProperty(nameof(IEvent.TransitionTime))]
        private TimeSpan _transitionTime;

        [JsonProperty(nameof(IEvent.TransitionType))]
        private TTransitionType _transitionType;

        private Lazy<IEvent> _parent;

        private Lazy<IEvent> _next;

        private Lazy<IEvent> _prior;

        private Lazy<IEnumerable<IEvent>> _subEvents;

        #pragma warning restore

        public Event()
        {
            _subEvents = new Lazy<IEnumerable<IEvent>>(() => Get<ReadOnlyCollection<IEvent>>(nameof(IEvent.SubEvents)));
            ResetSlibbings();
        }

        public ulong Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public double? AudioVolume { get { return _audioVolume; }  set { Set(value); } }

        public AutoStartFlags AutoStartFlags { get { return _autoStartFlags; } set { Set(value); } }

        public byte Crawl { get { return _crawl; } set { Set(value); } }

        public TimeSpan Duration { get { return _duration; } set { Set(value); } }
        
        public DateTime EndTime => _endTime;

        public IEngine Engine => _engine;

        public string EventName { get { return _eventName; } set { Set(value); } }

        public TEventType EventType => _eventType;
        
        public string IdAux { get { return _idAux; } set { Set(value); } }
        
        public ulong IdProgramme { get { return _idProgramme; } set { Set(value); } }

        public bool IsCGEnabled { get { return _isCGEnabled; } set { Set(value); } }

        public bool IsDeleted => _isDeleted;

        public bool IsEnabled { get { return _isEnabled; } set { Set(value); } }

        public bool IsForcedNext => _isForcedNext;

        public bool IsHold { get { return _isHold; } set { Set(value); } }

        public bool IsLoop { get { return _isLoop; } set { Set(value); } }

        public bool IsModified { get { return _isModified; } set { Set(value); } }

        public VideoLayer Layer { get { return _layer; } set { Set(value); } }

        public TimeSpan Length => _length;

        public byte Logo { get { return _logo; } set { Set(value); } }

        public IMedia Media { get { return _media; } set { Set(value); } }

        public Guid MediaGuid { get { return _mediaGuid; } set { Set(value); } }

        [JsonProperty]
        public IEvent Next
        {
            get { return _next.Value; }
            set
            {
                _next = new Lazy<IEvent>(() => value);
                Debug.Write($"New Next for: {this} is {value}");
            }
        }

        [JsonProperty]
        public IEvent Parent
        {
            get { return _parent.Value; }
            set
            {
                _parent = new Lazy<IEvent>(() => value); 
                Debug.Write($"New Parent for: {this} is {value}");
            }
        }

        [JsonProperty]
        public IEvent Prior
        {
            get { return _prior.Value; }
            set
            {
                _prior = new Lazy<IEvent>(() => value);
                Debug.Write($"New Prior for: {this} is {value}");
            }

        }

        public TimeSpan? Offset => _offset;

        public byte Parental { get { return _parental; } set { Set(value); } }

        public TPlayState PlayState => _playState;


        public TimeSpan? RequestedStartTime { get { return _requestedStartTime; }  set { Set(value); } }

        public TimeSpan ScheduledDelay { get { return _scheduledDelay; } set { Set(value); } }

        public TimeSpan ScheduledTc { get { return _scheduledTc; } set { Set(value); } }

        public DateTime ScheduledTime { get { return _scheduledTime; } set { Set(value); } }

        public TimeSpan StartTc { get { return _startTc; } set { Set(value); } }

        public DateTime StartTime => _startTime;

        public TStartType StartType { get { return _startType; } set { Set(value); } }

        public IEnumerable<IEvent> SubEvents => _subEvents.Value;

        public int SubEventsCount => _subEventsCount;

        public TEasing TransitionEasing { get { return _transitionEasing; } set { Set(value); } }

        public TimeSpan TransitionPauseTime { get { return _transitionPauseTime; } set { Set(value); } }

        public TimeSpan TransitionTime { get { return _transitionTime; } set { Set(value); } }

        public TTransitionType TransitionType { get { return _transitionType; } set { Set(value); } }

        #region Event handlers
        private event EventHandler _deleted;
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
        private event EventHandler<EventPositionEventArgs> _positionChanged;
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
        private event EventHandler _located;
        public event EventHandler Located
        {
            add
            {
                EventAdd(_located);
                _located += value;
            }
            remove
            {
                _located -= value;
                EventRemove(_located);
            }
        }
        private event EventHandler<CollectionOperationEventArgs<IEvent>> _subEventChanged;
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

        protected override void OnEventNotification(WebSocketMessage message)
        {
            switch (message.MemberName)
            {
                case nameof(IEvent.Deleted):
                    _deleted?.Invoke(this, Deserialize<EventArgs>(message));
                    break;
                case nameof(IEvent.PositionChanged):
                    _positionChanged?.Invoke(this, Deserialize<EventPositionEventArgs>(message));
                    break;
                case nameof(IEvent.Located):
                    _located?.Invoke(this, Deserialize<EventArgs>(message));
                    return;
                case nameof(IEvent.SubEventChanged):
                    var ea = Deserialize<CollectionOperationEventArgs<IEvent>>(message);
                    _subEventChanged?.Invoke(this, ea);
                    return;
            }
        }

        #endregion //Event handlers

        public bool AllowDelete() { return Query<bool>(); }

        public void Delete() { Invoke(); }

        public bool InsertAfter(IEvent e) { return Query<bool>(parameters: new object[] { e }); }

        public bool InsertBefore(IEvent e) { return Query<bool>(parameters: new object[] { e }); }

        public bool InsertUnder(IEvent se, bool fromEnd) { return Query<bool>(parameters: new object[] { se, fromEnd }); }

        public bool MoveDown() { return Query <bool>(); }

        public bool MoveUp() { return Query<bool>(); }

        public void Remove() { Invoke(); }

        public void Save()
        {
            Invoke();
        }

        public IEnumerable<IAclRight> GetRights() => Query<ReadOnlyCollection<EventAclRight>>();

        public IAclRight AddRightFor(ISecurityObject securityObject) { return Query<IAclRight>(parameters: new object[] { securityObject }); }

        public bool DeleteRight(IAclRight item) { return Query<bool>(parameters: new object[] { item }); }

        public bool HaveRight(EventRight right)
        {
            return Query<bool>(parameters: new object[] { right });
        }
        
        private void ResetSlibbings()
        {
            _parent = new Lazy<IEvent>(() => Get<Event>(nameof(IEvent.Parent)));
            _next = new Lazy<IEvent>(() => Get<Event>(nameof(IEvent.Next)));
            _prior = new Lazy<IEvent>(() => Get<Event>(nameof(IEvent.Prior)));
        }

        public override string ToString()
        {
            return $"{nameof(Event)}: {EventName}";
        }
    }
}
