using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.Security;
using TAS.Remoting.Model.Media;
using TAS.Remoting.Model.Security;

namespace TAS.Remoting.Model
{
    [DebuggerDisplay("{" + nameof(EventName) + "}")]
    public class Event : ProxyObjectBase, IEvent
    {
#pragma warning disable CS0649

        [DtoField(nameof(IEvent.Id))]
        private ulong _id;

        [DtoField(nameof(IEvent.AudioVolume))]
        private double? _audioVolume;

        [DtoField(nameof(IEvent.AutoStartFlags))]
        private AutoStartFlags _autoStartFlags;

        [DtoField(nameof(IEvent.Crawl))]
        private byte _crawl;

        [DtoField(nameof(IEvent.RouterPort))]
        private int _routerPort;

        [DtoField(nameof(IEvent.Duration))]
        private TimeSpan _duration;

        [DtoField(nameof(IEvent.EndTime))]
        private DateTime _endTime;

        [DtoField(nameof(IEvent.Engine))]
        private IEngine _engine;

        [DtoField(nameof(IEvent.EventName))]
        private string _eventName;

        [DtoField(nameof(IEvent.EventType))]
        private TEventType _eventType;

        [DtoField(nameof(IEvent.IdAux))]
        private string _idAux;

        [DtoField(nameof(IEvent.IdProgramme))]
        private ulong _idProgramme;

        [DtoField(nameof(IEvent.IsCGEnabled))]
        private bool _isCGEnabled;

        [DtoField(nameof(IEvent.IsDeleted))]
        private bool _isDeleted;

        [DtoField(nameof(IEvent.IsEnabled))]
        private bool _isEnabled;

        [DtoField(nameof(IEvent.IsForcedNext))]
        private bool _isForcedNext;

        [DtoField(nameof(IEvent.IsHold))]
        private bool _isHold;

        [DtoField(nameof(IEvent.IsLoop))]
        private bool _isLoop;

        [DtoField(nameof(IEvent.Layer))]
        private VideoLayer _layer;

        [DtoField(nameof(IEvent.Logo))]
        private byte _logo;

        [DtoField(nameof(IEvent.Media))]
        private MediaBase _media;

        [DtoField(nameof(IEvent.MediaGuid))]
        private Guid _mediaGuid;

        [DtoField(nameof(IEvent.Offset))]
        private TimeSpan? _offset;

        [DtoField(nameof(IEvent.Parental))]
        private byte _parental;

        [DtoField(nameof(IEvent.PlayState))]
        private TPlayState _playState;

        [DtoField(nameof(IEvent.RequestedStartTime))]
        private TimeSpan? _requestedStartTime;

        [DtoField(nameof(IEvent.ScheduledDelay))]
        private TimeSpan _scheduledDelay;

        [DtoField(nameof(IEvent.ScheduledTc))]
        private TimeSpan _scheduledTc;

        [DtoField(nameof(IEvent.ScheduledTime))]
        private DateTime _scheduledTime;

        [DtoField(nameof(IEvent.StartTc))]
        private TimeSpan _startTc;

        [DtoField(nameof(IEvent.StartTime))]
        private DateTime _startTime;

        [DtoField(nameof(IEvent.StartType))]
        private TStartType _startType;

        [DtoField(nameof(IEvent.SubEventsCount))]
        private int _subEventsCount;

        [DtoField(nameof(IEvent.TransitionEasing))]
        private TEasing _transitionEasing;

        [DtoField(nameof(IEvent.TransitionPauseTime))]
        private TimeSpan _transitionPauseTime;

        [DtoField(nameof(IEvent.TransitionTime))]
        private TimeSpan _transitionTime;

        [DtoField(nameof(IEvent.TransitionType))]
        private TTransitionType _transitionType;

        private Lazy<IEvent> _parent;

        private Lazy<IEvent> _next;

        private Lazy<IEvent> _prior;

        private Lazy<List<IEvent>> _subEvents;

        [DtoField(nameof(IEvent.CurrentUserRights))]
        private ulong _currentUserRights;

        [DtoField(nameof(IEvent.RecordingInfo))]
        private RecordingInfo _recordingInfo;

#pragma warning restore

        public Event()
        {
            ResetSubEvents();
            ResetSlibbings();
        }

        public ulong Id { get => _id; set => _id = value; }

        public double? AudioVolume { get => _audioVolume; set => Set(value); }

        public AutoStartFlags AutoStartFlags { get => _autoStartFlags; set => Set(value); }

        public byte Crawl { get => _crawl; set => Set(value); }

        public int RouterPort { get => _routerPort; set => Set(value); }

        public TimeSpan Duration { get => _duration; set => Set(value); }

        public DateTime EndTime => _endTime;

        public IEngine Engine => _engine;

        public string EventName { get => _eventName; set => Set(value); }

        public TEventType EventType => _eventType;

        public string IdAux { get => _idAux; set => Set(value); }

        public ulong IdProgramme { get => _idProgramme; set => Set(value); }

        public bool IsCGEnabled { get => _isCGEnabled; set => Set(value); }

        public bool IsDeleted => _isDeleted;

        public bool IsEnabled { get => _isEnabled; set => Set(value); }

        public bool IsForcedNext => _isForcedNext;

        public bool IsHold { get => _isHold; set => Set(value); }

        public bool IsLoop { get => _isLoop; set => Set(value); }

        public VideoLayer Layer { get => _layer; set => Set(value); }

        public byte Logo { get => _logo; set => Set(value); }

        public IMedia Media { get => _media; set => Set(value); }

        public Guid MediaGuid { get => _mediaGuid; set => Set(value); }

        public IEvent Next
        {
            get => _next.Value;
            set
            {
                _next = new Lazy<IEvent>(() => value);
                Debug.Write($"New Next for: {this} is {value}");
            }
        }

        public IEvent Parent
        {
            get => _parent.Value;
            set
            {
                _parent = new Lazy<IEvent>(() => value);
                Debug.Write($"New Parent for: {this} is {value}");
            }
        }

        public IEvent Prior
        {
            get => _prior.Value;
            set
            {
                _prior = new Lazy<IEvent>(() => value);
                Debug.Write($"New Prior for: {this} is {value}");
            }

        }

        public TimeSpan? Offset => _offset;

        public byte Parental { get => _parental; set => Set(value); }

        public TPlayState PlayState => _playState;


        public TimeSpan? RequestedStartTime { get => _requestedStartTime; set => Set(value); }

        public TimeSpan ScheduledDelay { get => _scheduledDelay; set => Set(value); }

        public TimeSpan ScheduledTc { get => _scheduledTc; set => Set(value); }

        public DateTime ScheduledTime { get => _scheduledTime; set => Set(value); }

        public TimeSpan StartTc { get => _startTc; set => Set(value); }

        public DateTime StartTime => _startTime;

        public TStartType StartType { get => _startType; set => Set(value); }

        public IEnumerable<IEvent> SubEvents => _subEvents.Value;

        public int SubEventsCount => _subEventsCount;

        public TEasing TransitionEasing { get => _transitionEasing; set => Set(value); }

        public TimeSpan TransitionPauseTime { get => _transitionPauseTime; set => Set(value); }

        public TimeSpan TransitionTime { get => _transitionTime; set => Set(value); }

        public TTransitionType TransitionType { get => _transitionType; set => Set(value); }

        public RecordingInfo RecordingInfo { get => _recordingInfo; set => Set(value); }

        public IEvent GetSuccessor()
        {
            return Query<Event>();
        }

        #region Event handlers
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

        protected override void OnEventNotification(SocketMessage message)
        {
            switch (message.MemberName)
            {
                case nameof(IEvent.PositionChanged):
                    _positionChanged?.Invoke(this, Deserialize<EventPositionEventArgs>(message));
                    break;
                case nameof(IEvent.SubEventChanged):
                    var ea = Deserialize<CollectionOperationEventArgs<IEvent>>(message);
                    if (!_subEvents.IsValueCreated)
                        return;
                    if (ea.Operation == CollectionOperation.Add)
                        _subEvents.Value.Add(ea.Item);
                    else
                        _subEvents.Value.Remove(ea.Item);
                    _subEventChanged?.Invoke(this, ea);
                    break;
            }
        }

        #endregion //Event handlers

        public bool AllowDelete() { return Query<bool>(); }

        public void Delete() { Invoke(); }

        public bool InsertAfter(IEvent e) { return Query<bool>(parameters: new object[] { e }); }

        public bool InsertBefore(IEvent e) { return Query<bool>(parameters: new object[] { e }); }

        public bool InsertUnder(IEvent se, bool fromEnd) { return Query<bool>(parameters: new object[] { se, fromEnd }); }

        public bool MoveDown() { return Query<bool>(); }

        public bool MoveUp() { return Query<bool>(); }

        public void Remove() { Invoke(); }

        public IDictionary<string, int> FieldLengths => _engine.EventFieldLengths;

        public void Save()
        {
            Invoke();
        }

        public IEnumerable<IAclRight> GetRights() => Query<ReadOnlyCollection<EventAclRight>>();

        public IAclRight AddRightFor(ISecurityObject securityObject) { return Query<IAclRight>(parameters: new object[] { securityObject }); }

        public void DeleteRight(IAclRight item) { Invoke(parameters: new object[] { item }); }

        public ulong CurrentUserRights => _currentUserRights;        
    

    public bool HaveRight(EventRight right)
        {
            if (_engine.HaveRight(EngineRight.Rundown))
                return true;
            return (CurrentUserRights & (ulong)right) > 0;
        }

        private void ResetSlibbings()
        {
            _parent = new Lazy<IEvent>(() => Get<Event>(nameof(IEvent.Parent)));
            _next = new Lazy<IEvent>(() => Get<Event>(nameof(IEvent.Next)));
            _prior = new Lazy<IEvent>(() => Get<Event>(nameof(IEvent.Prior)));
        }

        private void ResetSubEvents()
        {
            _subEvents = new Lazy<List<IEvent>>(() => Get<List<IEvent>>(nameof(IEvent.SubEvents)));
        }

        public override string ToString()
        {
            return $"{nameof(Event)}: {EventName}";
        }
    }
}
