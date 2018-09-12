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

        [JsonProperty(nameof(IEvent.Layer))]
        private VideoLayer _layer;

        [JsonProperty(nameof(IEvent.Logo))]
        private byte _logo;

        [JsonProperty(nameof(IEvent.Media))]
        private MediaBase _media;

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

        private Lazy<List<IEvent>> _subEvents;

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

        [JsonProperty]
        public IEvent Next
        {
            get => _next.Value;
            set
            {
                _next = new Lazy<IEvent>(() => value);
                Debug.Write($"New Next for: {this} is {value}");
            }
        }

        [JsonProperty]
        public IEvent Parent
        {
            get => _parent.Value;
            set
            {
                _parent = new Lazy<IEvent>(() => value);
                Debug.Write($"New Parent for: {this} is {value}");
            }
        }

        [JsonProperty]
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
                    if (!_subEvents.IsValueCreated)
                        return;
                    var ea = Deserialize<CollectionOperationEventArgs<IEvent>>(message);
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
