using jNet.RPC;
using jNet.RPC.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.Security;
using TAS.Server.Media;
using TAS.Server.Security;

namespace TAS.Server
{
    [DebuggerDisplay("{" + nameof(_eventName) + "}")]
    public class Event : ServerObjectBase, IEventPersistent
    {
        [DtoMember(nameof(IEventPersistent.Engine))]
        private readonly Engine _engine;
        private bool _isForcedNext;
        private TPlayState _playState;
        private long _position;
        private readonly Lazy<List<IEvent>> _subEvents;
        private Lazy<Event> _parent;
        private Lazy<Event> _prior;
        private Lazy<Event> _next;
        private readonly Lazy<List<IAclRight>> _rights;
        private bool _isCGEnabled;
        private byte _crawl;
        private byte _logo;
        private byte _parental;
        private int _routerPort = -1;
        private RecordingInfo _recordingInfo;
        private double? _audioVolume;
        private TimeSpan _duration;
        private bool _isEnabled;
        string _eventName;
        private TEventType _eventType;
        private bool _isHold;
        private bool _isLoop;
        private string _idAux;
        private ulong _idProgramme;
        private VideoLayer _layer;
        private TimeSpan? _requestedStartTime;
        private TimeSpan _scheduledDelay;
        private TimeSpan _scheduledTc;
        private DateTime _scheduledTime;
        private DateTime _startTime;
        private TimeSpan _startTc;
        private TStartType _startType;
        private TimeSpan _transitionTime;
        private TimeSpan _transitionPauseTime;
        private TTransitionType _transitionType;
        private TEasing _transitionEasing;
        private AutoStartFlags _autoStartFlags;
        private Guid _mediaGuid;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IRouterPort _inputPort;
        private bool _isDeleted;

        #region Constructor
        internal Event(
                    Engine engine,
                    UInt64 idRundownEvent,
                    UInt64 idEventBinding,
                    VideoLayer videoLayer,
                    TEventType eventType,
                    TStartType startType,
                    TPlayState playState,
                    DateTime scheduledTime,
                    TimeSpan duration,
                    TimeSpan scheduledDelay,
                    TimeSpan scheduledTC,
                    Guid mediaGuid,
                    string eventName,
                    DateTime startTime,
                    TimeSpan startTC,
                    TimeSpan? requestedStartTime,
                    TimeSpan transitionTime,
                    TimeSpan transitionPauseTime, 
                    TTransitionType transitionType,
                    TEasing transitionEasing,
                    double? audioVolume,
                    UInt64 idProgramme,
                    string idAux,
                    bool isEnabled,
                    bool isHold,
                    bool isLoop,
                    AutoStartFlags autoStartFlags,
                    bool isCGEnabled,
                    byte crawl,
                    byte logo,
                    byte parental,
                    short routerPort,
                    RecordingInfo recordingInfo)
        {
            _engine = engine;
            Id = idRundownEvent;
            IdEventBinding = idEventBinding;
            _layer = videoLayer;
            _eventType = eventType;
            _startType = startType;
            _playState = playState == TPlayState.Paused ? TPlayState.Scheduled: playState == TPlayState.Fading ? TPlayState.Played : playState;
            _scheduledTime = scheduledTime;
            _duration = duration;
            _scheduledDelay = scheduledDelay;
            _scheduledTc = scheduledTC;
            _eventName = eventName;
            _startTime = startTime;
            _startTc = startTC;
            _requestedStartTime = requestedStartTime;
            _transitionTime = transitionTime;
            _transitionPauseTime = transitionPauseTime;
            _transitionType = transitionType;
            _transitionEasing = transitionEasing;
            _audioVolume = audioVolume;
            _idProgramme = idProgramme;
            _idAux = idAux;
            _isEnabled = isEnabled;
            _isHold = isHold;
            _isLoop = isLoop;
            _isCGEnabled = isCGEnabled;
            _crawl = crawl;
            _logo = logo;
            _parental = parental;
            _autoStartFlags = autoStartFlags;
            _mediaGuid = mediaGuid;
            _subEvents = new Lazy<List<IEvent>>(() =>
            {
                var result = DatabaseProvider.Database.ReadSubEvents(_engine, this);
                foreach (Event e in result)
                    e.SetParent(this);
                return result;
            });

            _next = new Lazy<Event>(() =>
            {
                var next = (Event)DatabaseProvider.Database.ReadNext(_engine, this);
                if (next != null)
                    next.SetPrior(this);
                return next;
            });

            _prior = new Lazy<Event>(() =>
            {
                Event prior = null;
                if (startType == TStartType.After && IdEventBinding > 0)
                    prior = (Event)DatabaseProvider.Database.ReadEvent(_engine, IdEventBinding);
                if (prior != null)
                    prior.SetNext(this);
                return prior;
            });

            _parent = new Lazy<Event>(() =>
            {
                if ((startType == TStartType.WithParent || startType == TStartType.WithParentFromEnd) && IdEventBinding > 0)
                    return (Event)DatabaseProvider.Database.ReadEvent(_engine, IdEventBinding);
                return null;
            });

            _rights = new Lazy<List<IAclRight>>(() =>
            {
                var rights = DatabaseProvider.Database.ReadEventAclList<EventAclRight>(this, _engine.AuthenticationService as IAuthenticationServicePersitency);
                rights.ForEach(r => ((EventAclRight)r).Saved += AclEvent_Saved);
                return rights;
            });
            _routerPort = routerPort;
            _recordingInfo = recordingInfo;

            FieldLengths = DatabaseProvider.Database.EventFieldLengths;
        }
        #endregion //Constructor

#if DEBUG
        ~Event()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif

        #region IEventPesistent 
        [DtoMember]
        public ulong Id {get; set; }

        public ulong IdEventBinding { get; private set; }

        #endregion

        #region IEventProperties

        [DtoMember]
        public double? AudioVolume
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        [DtoMember]
        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                var oldDuration = _duration;
                value = ((Engine)Engine).AlignTimeSpan(value);
                if (!SetField(ref _duration, value, nameof(Duration)))
                    return;
                lock (_engine.RundownSync)
                {
                    if (_eventType == TEventType.Live || _eventType == TEventType.Movie)
                    {
                        foreach (Event e in _subEvents.Value.Where(ev => ev.EventType == TEventType.StillImage))
                        {
                            var nd = e._duration + value - oldDuration;
                            e.Duration = nd > TimeSpan.Zero ? nd : TimeSpan.Zero;
                        }
                    }
                    _durationChanged();
                }
            }
        }

        [DtoMember]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (!SetField(ref _isEnabled, value))
                    return;
                lock (_engine.RundownSync)
                {
                    _durationChanged();
                }
            }
        }

        [DtoMember]
        public string EventName
        {
            get => _eventName;
            set => SetField(ref _eventName, value);
        }

        [DtoMember]
        public TEventType EventType
        {
            get => _eventType;
            set
            {
                if (!SetField(ref _eventType, value))
                    return;
                if (value == TEventType.Live || value == TEventType.Rundown)
                    MediaGuid = Guid.Empty;
            }
        }

        [DtoMember]
        public bool IsHold
        {
            get => _isHold;
            set => SetField(ref _isHold, value);
        }

        [DtoMember]
        public bool IsLoop
        {
            get => _isLoop;
            set => SetField(ref _isLoop, value);
        }

        [DtoMember]
        public string IdAux
        {
            get => _idAux;
            set => SetField(ref _idAux, value);
        }

        [DtoMember]
        public ulong IdProgramme
        {
            get => _idProgramme;
            set => SetField(ref _idProgramme, value);
        }

        [DtoMember]
        public VideoLayer Layer { get => _layer; set => SetField(ref _layer, value); }

        [DtoMember]
        public TimeSpan? RequestedStartTime
        {
            get => _requestedStartTime;
            set
            {
                if (!SetField(ref _requestedStartTime, value))
                    return;
                NotifyPropertyChanged(nameof(Offset));
            }
        }

        [DtoMember]
        public TimeSpan ScheduledDelay
        {
            get => _scheduledDelay;
            set => SetField(ref _scheduledDelay, ((Engine) Engine).AlignTimeSpan(value));
        }

        [DtoMember]
        public TimeSpan ScheduledTc
        {
            get => _scheduledTc;
            set => SetField(ref _scheduledTc, ((Engine) Engine).AlignTimeSpan(value));
        }

        [DtoMember]
        public DateTime ScheduledTime
        {
            get => _scheduledTime;
            set
            {
                if (_startType == TStartType.Manual || _startType == TStartType.OnFixedTime && _playState == TPlayState.Scheduled)
                    lock(_engine.RundownSync)
                    {
                        _setScheduledTime(_engine.AlignDateTime(value));
                    }
            }
        }

        [DtoMember]
        public DateTime StartTime
        {
            get => _startTime;
            internal set
            {
                if (!SetField(ref _startTime, value))
                    return;
                if (value != default(DateTime))
                    lock (_engine.RundownSync)
                    {
                        _setScheduledTime(value);
                    }
            }
        }

        [DtoMember]
        public TStartType StartType
        {
            get => _startType;
            set
            {
                var oldValue = _startType;
                if (SetField(ref _startType, value))
                {
                    if (value == TStartType.OnFixedTime)
                        _engine.AddFixedTimeEvent(this);
                    if (oldValue == TStartType.OnFixedTime)
                        _engine.RemoveFixedTimeEvent(this);
                }
            }
        }

        [DtoMember]
        public TimeSpan TransitionTime
        {
            get => _transitionTime;
            set
            {
                if (SetField(ref _transitionTime, ((Engine)Engine).AlignTimeSpan(value)))
                {
                    lock (_engine.RundownSync)
                    {
                        _uppdateScheduledTime();
                        _durationChanged();
                    }
                }
            }
        }

        [DtoMember]
        public TimeSpan TransitionPauseTime
        {
            get => _transitionPauseTime;
            set => SetField(ref _transitionPauseTime, ((Engine)Engine).AlignTimeSpan(value));
        }

        [DtoMember]
        public TTransitionType TransitionType
        {
            get => _transitionType;
            set => SetField(ref _transitionType, value);
        }

        [DtoMember]
        public TEasing TransitionEasing
        {
            get => _transitionEasing;
            set => SetField(ref _transitionEasing, value);
        }

        [DtoMember]
        public AutoStartFlags AutoStartFlags
        {
            get => _autoStartFlags;
            set => SetField(ref _autoStartFlags, value);
        }

        [DtoMember]
        public Guid MediaGuid
        {
            get => _mediaGuid;
            set
            {
                if (!SetField(ref _mediaGuid, value))
                    return;
                NotifyPropertyChanged(nameof(Media));
            }
        }

        #endregion //IEventProperties

        #region IAclObject

        public IEnumerable<IAclRight> GetRights()
        {
            lock (_rights) return _rights.Value.ToArray();
        }

        public IAclRight AddRightFor(ISecurityObject securityObject)
        {
            var right = new EventAclRight { Owner = this, SecurityObject = securityObject };
            lock (_rights)
            {
                _rights.Value.Add(right);
                right.Saved += AclEvent_Saved;
            }
            return right;
        }

        public void DeleteRight(IAclRight item)
        {
            var right = (AclRightBase)item;
            lock (_rights)
            {
                var success = _rights.Value.Remove(right);
                if (!success)
                    return;
                right.Delete();
                right.Saved -= AclEvent_Saved;
            }
        }

        [DtoMember]
        public ulong CurrentUserRights
        {
            get
            {
                if (!(Thread.CurrentPrincipal.Identity is IUser user))
                    return 0UL;
                if (user.IsAdmin)
                {
                    var values = Enum.GetValues(typeof(EventRight)).Cast<ulong>();
                    return values.Aggregate<ulong, ulong>(0, (current, value) => current | value);
                }
                var acl = _getVisualParent()?.CurrentUserRights ?? 0;
                var groups = user.GetGroups();
                lock (_rights)
                {
                    var userRights = _rights.Value.Where(r => r.SecurityObject == user);
                    acl = userRights.Aggregate(acl, (current, right) => current | right.Acl);
                    var groupRights = _rights.Value.Where(r => groups.Any(g => g == r.SecurityObject));
                    acl = groupRights.Aggregate(acl, (current, groupRight) => current | groupRight.Acl);
                }
                return acl;
            }
        }

        #endregion // IAclObject

        [DtoMember]
        public bool IsForcedNext
        {
            get => _isForcedNext;
            internal set => SetField(ref _isForcedNext, value);
        }

        [DtoMember]
        public RecordingInfo RecordingInfo
        {
            get => _recordingInfo;
            set => SetField(ref _recordingInfo, value);
        }

        public bool IsModified { get; set; }

        [DtoMember]
        public virtual TPlayState PlayState
        {
            get => _playState;
            set
            {
                lock (_engine.RundownSync)
                {
                    _setPlayState(value);
                }
            }
        }

        [DtoMember]
        public long Position // in frames
        {
            get => _position;
            set
            {
                if (_position == value)
                    return;
                _position = value;
                PositionChanged?.Invoke(this,
                    PlayState == TPlayState.Scheduled
                        ? new EventPositionEventArgs(value, TimeSpan.Zero)
                        : new EventPositionEventArgs(value, _duration - TimeSpan.FromTicks(Engine.FrameTicks * value)));
            }
        }

        public IEnumerable<IEvent> GetSubEvents() { lock (_engine.RundownSync) return _subEvents.Value.ToArray(); } 

        [DtoMember]
        public int SubEventsCount { get { lock (_engine.RundownSync) { return _subEvents.Value.Count; } } }

        public IEngine Engine => _engine;

        internal TimeSpan Length => _isEnabled ? _duration : TimeSpan.Zero;
        internal long LengthInFrames => Length.Ticks / Engine.FrameTicks;
        
        [DtoMember]
        public DateTime EndTime => _scheduledTime + Length;

        [DtoMember]
        public TimeSpan StartTc
        {
            get => _startTc;
            set
            {
                value = ((Engine)Engine).AlignTimeSpan(value);
                SetField(ref _startTc, value);
            }
        }

        [DtoMember]
        public IMedia Media
        {
            get
            {
                if (MediaGuid == Guid.Empty)
                    return null;
                return ServerMediaPRI ?? ServerMediaSEC;
            }
            set => MediaGuid = value?.MediaGuid ?? Guid.Empty;
        }

        public IEvent GetParent() => _parent.Value;

        private void SetParent(IEvent value)
        {
            _parent = new Lazy<Event>(() => (Event)value);
            IsModified = true;
        }

        public IEvent GetPrior() => _prior.Value;

        private void SetPrior(IEvent value)
        {
            _prior = new Lazy<Event>(() => (Event)value);
            IsModified = true;
        }

        public IEvent GetNext() => _next.Value;

        private void SetNext(IEvent value)
        {
            _next = new Lazy<Event>(() => (Event)value);
            IsModified = true;
            if (value != null)
                IsLoop = false;
        }

        public IEvent GetVisualParent()
        {
            IEvent curr = this;
            var prior = curr.GetPrior();
            while (prior != null)
            {
                curr = prior;
                prior = curr.GetPrior();
            }
            return curr.GetParent();
        }

        [DtoMember]
        public TimeSpan? Offset
        {
            get
            {
                var rrt = _requestedStartTime;
                if (rrt != null)
                    return _scheduledTime.ToLocalTime().TimeOfDay - rrt;
                return null;
            }
        }

        [DtoMember]
        public bool IsDeleted
        {
            get => _isDeleted;
            private set => SetField(ref _isDeleted, value);
        }

        [DtoMember]
        public bool IsCGEnabled
        {
            get => _isCGEnabled;
            set => SetField(ref _isCGEnabled, value);
        }

        [DtoMember]
        public IRouterPort InputPort
        {
            get => _inputPort;
            set => SetField(ref _inputPort, value);
        }

        [DtoMember]
        public byte Crawl
        {
            get => _crawl;
            set => SetField(ref _crawl, value);
        }

        [DtoMember]
        public byte Logo
        {
            get => _logo;
            set => SetField(ref _logo, value);
        }

        [DtoMember]
        public byte Parental
        {
            get => _parental;
            set => SetField(ref _parental, value);
        }
                     
        [DtoMember]
        public int RouterPort
        {
            get => _routerPort;
            set => SetField(ref _routerPort, value);
        }       

        public event EventHandler<EventPositionEventArgs> PositionChanged;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;

        public void Remove()
        {
            lock(_engine.RundownSync)
            {
                _remove();
            }
        }

        private void _remove()
        {
            Event next;
            var parent = GetParent() as Event;
            next = GetNext() as Event;
            var prior = GetPrior() as Event;
            var startType = _startType;
            _engine.RemoveRootEvent(this);
            if (next != null)
            {
                next.SetParent(parent);
                next.SetPrior(prior);
                next.StartType = startType;
                if (prior == null)
                    next._updateScheduledTimeWithSuccessors();
            }
            if (parent != null)
            {
                parent._subEventsRemove(this);
                if (next != null)
                {
                    lock (parent._subEvents.Value.SyncRoot())
                    {
                        parent._subEvents.Value.Add(next);
                    }
                    parent.NotifyPropertyChanged(nameof(SubEventsCount));
                }
                if (parent.SetField(ref parent._duration, parent._computedDuration(), nameof(Duration)))
                    parent._durationChanged();
                if (next != null)
                    parent.NotifySubEventChanged(next, CollectionOperation.Add);
            }
            if (prior != null)
            {
                prior.SetNext(next);
                prior._durationChanged();
            }
            next?.Save();
            SetNext(null);
            SetPrior(null);
            SetParent(null);
            IdEventBinding = 0;
            StartType = TStartType.None;
        }

        public bool MoveUp()
        {
            Event e2;
            Event e4;
            lock (_engine.RundownSync)
            {
                // this = e3
                e2 = GetPrior() as Event;
                e4 = GetNext() as Event; // load if nescessary
                if (e2 == null)
                    return false;
                var e2Parent = e2.GetParent() as Event;
                var e2Prior = e2.GetPrior() as Event;
                if (e2Parent != null)
                {
                    lock (e2Parent._subEvents.Value.SyncRoot())
                    {
                        var index = e2Parent._subEvents.Value.IndexOf(e2);
                        e2Parent._subEvents.Value[index] = this;
                    }
                    e2Parent.NotifySubEventChanged(e2, CollectionOperation.Remove);
                    e2Parent.NotifySubEventChanged(this, CollectionOperation.Add);
                }
                if (e2Prior != null)
                    e2Prior.SetNext(this);
                StartType = e2._startType;
                AutoStartFlags = e2.AutoStartFlags;
                SetPrior(e2Prior);
                SetParent(e2Parent);
                IdEventBinding = e2.IdEventBinding;
                e2.SetPrior(this);
                e2.StartType = TStartType.After;
                e2.SetNext(e4);
                e2.SetParent(null);
                SetNext(e2);
                if (e4 != null)
                    e4.SetPrior(e2);
                _uppdateScheduledTime();
                e2._uppdateScheduledTime();
            }
            e4?.Save();
            e2.Save();
            Save();
            NotifyLocated();
            return true;
        }

        public bool MoveDown()
        {
            Event e3;
            Event e4;
            lock (_engine.RundownSync)
            {
                // this = e2
                e3 = GetNext() as Event; // load if nescessary
                if (e3 == null)
                    return false;
                e4 = e3.GetNext() as Event;
                var e2Parent = GetParent() as Event;
                var e2Prior = GetPrior() as Event;
                if (e2Parent != null)
                {
                    lock (e2Parent._subEvents.Value.SyncRoot())
                    {
                        var index = e2Parent._subEvents.Value.IndexOf(this);
                        e2Parent._subEvents.Value[index] = e3;
                    }
                    e2Parent.NotifySubEventChanged(this, CollectionOperation.Remove);
                    e2Parent.NotifySubEventChanged(e3, CollectionOperation.Add);
                }
                if (e2Prior != null)
                    e2Prior.SetNext(e3);
                e3.StartType = _startType;
                e3.AutoStartFlags = _autoStartFlags;
                e3.SetPrior(e2Prior);
                e3.SetParent(e2Parent);
                e3.IdEventBinding = IdEventBinding;
                StartType = TStartType.After;
                e3.SetNext(this);
                SetParent(null);
                SetNext(e4);
                SetPrior(e3);
                if (e4 != null)
                    e4.SetPrior(this);
                e3._uppdateScheduledTime();
                _uppdateScheduledTime();
            }
            e4?.Save();
            Save();
            e3.Save();
            NotifyLocated();
            return true;
        }

        public bool InsertAfter(IEvent e)
        {
            var eventToInsert = (Event) e;
            Event next;
            lock (_engine.RundownSync)
            {
                var oldParent = eventToInsert.GetParent() as Event;
                var oldPrior = eventToInsert.GetPrior() as Event;
                oldParent?._subEventsRemove(eventToInsert);
                if (oldPrior != null)
                    oldPrior.SetNext(null);

                next = this.GetNext() as Event;
                if (next == eventToInsert)
                    return false;
                this.SetNext(eventToInsert);
                eventToInsert.StartType = TStartType.After;
                eventToInsert.SetPrior(this);

                eventToInsert.SetNext(next);

                if (next != null)
                    next.SetPrior(eventToInsert);
 
                //time calculations
                eventToInsert._uppdateScheduledTime();
                eventToInsert._durationChanged();
            }
            // notify about relocation
            eventToInsert.NotifyLocated();

            // save key events
            eventToInsert.Save();
            next?.Save();
            return true;
        }

        public bool InsertBefore(IEvent e)
        {
            var eventToInsert = (Event) e;
            lock (_engine.RundownSync)
            {
                var prior = this.GetPrior() as Event;
                var parent = this.GetParent() as Event;
                var oldParent = eventToInsert.GetParent() as Event;
                var oldPrior = eventToInsert.GetPrior() as Event;
                oldParent?._subEventsRemove(eventToInsert);
                if (oldPrior != null)
                    oldPrior.SetNext(null);

                eventToInsert.StartType = _startType;
                if (prior == null)
                    eventToInsert.IsHold = false;

                if (parent != null)
                {
                    parent._subEvents.Value.Remove(this);
                    parent._subEvents.Value.Add(eventToInsert);
                    parent.NotifySubEventChanged(eventToInsert, CollectionOperation.Add);
                    SetParent(null);
                }
                eventToInsert.SetParent(parent);
                eventToInsert.SetPrior(prior);

                if (prior != null)
                    prior.SetNext(eventToInsert);

                this.SetPrior(eventToInsert);
                eventToInsert.SetNext(this);
                this.StartType = TStartType.After;

                // time calculations
                eventToInsert._uppdateScheduledTime();
                eventToInsert._durationChanged();
            }
            // notify about relocation
            eventToInsert.NotifyLocated();

            eventToInsert.Save();
            Save();
            return true;
        }

        public bool InsertUnder(IEvent se, bool fromEnd)
        {
            var subEventToAdd = (Event) se;
            lock (_engine.RundownSync)
            {
                var oldPrior = subEventToAdd.GetPrior() as Event;
                var oldParent = subEventToAdd.GetParent() as Event;
                oldParent?._subEventsRemove(subEventToAdd);
                if (oldPrior != null)
                    oldPrior.SetNext(null);
                if (EventType == TEventType.Container)
                {
                    if (!(subEventToAdd.StartType == TStartType.Manual ||
                          subEventToAdd.StartType == TStartType.OnFixedTime)) // do not change if valid
                        subEventToAdd.StartType = TStartType.Manual;
                }
                else
                    subEventToAdd.StartType = fromEnd ? TStartType.WithParentFromEnd : TStartType.WithParent;
                subEventToAdd.SetParent(this);
                subEventToAdd.IsHold = false;
                lock (_subEvents.Value.SyncRoot())
                {
                    _subEvents.Value.Add(subEventToAdd);
                }
                NotifyPropertyChanged(nameof(SubEventsCount));
                NotifySubEventChanged(subEventToAdd, CollectionOperation.Add);
                if (_eventType == TEventType.Rundown)
                    Duration = _computedDuration();
                if (subEventToAdd.GetPrior() is Event prior)
                {
                    prior.SetNext(null);
                    subEventToAdd.SetPrior(null);
                    prior._updateScheduledTimeWithSuccessors();
                }
            }
            subEventToAdd._updateScheduledTimeWithSuccessors();
            
            // notify about relocation
            subEventToAdd.NotifyLocated();
            if (IdEventBinding == 0)
                Save();
            subEventToAdd.Save();
            return true;
        }

        /// <summary>
        /// Gets time of event that requires attention event, or null if event does not contain such an element
        /// </summary>
        /// <returns></returns> 
        public TimeSpan? GetAttentionTime()
        {
            if (_isHold || _eventType == TEventType.Live)
                return TimeSpan.Zero;
            if (_eventType == TEventType.Movie)
            {
                IMedia m = Media;
                if (m == null
                    || m.MediaStatus != TMediaStatus.Available
                    || _scheduledTc < m.TcStart
                    || _duration + _scheduledTc > m.Duration + m.TcStart)
                    return TimeSpan.Zero;
            }
            if (_eventType == TEventType.Rundown)
            {
                TimeSpan pauseTime = TimeSpan.Zero;
                Event ev = GetSubEvents().FirstOrDefault(e => e.EventType == TEventType.Movie || e.EventType == TEventType.Live || e.EventType == TEventType.Rundown) as Event;
                while (ev != null)
                {
                    TimeSpan? pt = ev.GetAttentionTime();
                    if (pt.HasValue)
                        return pauseTime + pt.Value;
                    pauseTime += ev.Length - ev.TransitionTime;
                    ev = ev.GetNext() as Event;
                }
            }
            return null;
        }
        
        public void Delete()
        {
            if (IsDeleted || !AllowDelete())
                return;
            lock (_engine.RundownSync)
            {
                foreach (var e in this._getSubEventTree().ToArray())
                    e._delete();
                _delete();
            }
        }

        private IEnumerable<Event> _getSubEventTree()
        {
            foreach (var e in _subEvents.Value.Cast<Event>())
            {
                var nev = e;
                while (nev != null)
                {
                    foreach (var ev in nev._getSubEventTree())
                        yield return ev;
                    yield return nev;
                    nev = nev._next.Value;
                }
            }
        }

        public MediaDeleteResult CheckCanDeleteMedia(IServerMedia media)
        {
            if (EventType == TEventType.Rundown && (PlayState == TPlayState.Played || EndTime < DateTime.Now))
                return MediaDeleteResult.NoDeny;
            Event firstEvent;
            lock (_engine.RundownSync)
            {
                firstEvent = _getSubEventTree().FirstOrDefault(e =>
                        e.EventType == TEventType.Movie &&
                        e.Media == media &&
                        e.PlayState != TPlayState.Played);
            }
            return firstEvent is null ?
                MediaDeleteResult.NoDeny :
                new MediaDeleteResult { Result = MediaDeleteResult.MediaDeleteResultEnum.InSchedule, Event = firstEvent, Media = media };
        }

        public IDictionary<string, int> FieldLengths { get; }

        public void Save()
        {
            switch (_startType)
            {
                case TStartType.After:
                    IdEventBinding = GetPrior()?.Id ?? 0;
                    break;
                default:
                    IdEventBinding = GetParent()?.Id ?? 0;
                    break;
            }
            try
            {
                if (Id == 0)
                    DatabaseProvider.Database.InsertEvent(this);
                else
                    DatabaseProvider.Database.UpdateEvent(this);
                IsModified = false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception saving event {0}", EventName);
            }
        }

        public bool AllowDelete()
        {
            if (!HaveRight(EventRight.Delete))
                return false;
            if ((_playState == TPlayState.Fading || _playState == TPlayState.Paused || _playState == TPlayState.Playing) &&
                (_eventType == TEventType.Live || _eventType == TEventType.Movie || _eventType == TEventType.Rundown))
                return false;
            if (_eventType == TEventType.Container && GetSubEvents().Any())
                return false;
            foreach (var ne in this._getSubEventTree())
            {
                if (!ne.AllowDelete())
                    return false;
            }
            return true;
        }

        public bool HaveRight(EventRight right)
        {
            if (_engine.HaveRight(EngineRight.Rundown))
                return true;
            return (CurrentUserRights & (ulong)right) > 0;
        }
        
        public override string ToString()
        {
            return $"Event {EventType} {EventName}";
        }

        internal PersistentMedia ServerMediaPRI => (_eventType == TEventType.Animation ? (WatcherDirectory)Engine.MediaManager.AnimationDirectoryPRI : (WatcherDirectory)Engine.MediaManager.MediaDirectoryPRI)?.FindMediaByMediaGuid(MediaGuid) as PersistentMedia;

        internal PersistentMedia ServerMediaSEC => (_eventType == TEventType.Animation ? (WatcherDirectory)Engine.MediaManager.AnimationDirectorySEC: (WatcherDirectory)Engine.MediaManager.MediaDirectorySEC)?.FindMediaByMediaGuid(MediaGuid) as PersistentMedia;

        internal PersistentMedia ServerMediaPRV => (_eventType == TEventType.Animation ? (WatcherDirectory)Engine.MediaManager.AnimationDirectoryPRV : (WatcherDirectory)Engine.MediaManager.MediaDirectoryPRV)?.FindMediaByMediaGuid(MediaGuid) as PersistentMedia;

        internal void SaveLoadedTree()
        {
            var stopWatch = new Stopwatch();
            lock (_engine.RundownSync)
            {
                stopWatch.Start();
                _saveLoadedTree(this);
                stopWatch.Stop();
                NLog.LogMessageGenerator logMessageFunc = () => $"{nameof(SaveLoadedTree)} executed for {EventName}. It took {stopWatch.ElapsedMilliseconds} ms.";
                if (stopWatch.ElapsedMilliseconds > 100)
                    Logger.Warn(logMessageFunc);
                else
                    Logger.Debug(logMessageFunc);
            }
        }

        private static void _saveLoadedTree(Event ev)
        {
            while (!(ev is null))
            {
                if (ev.IsModified)
                    ev.Save();
                if (ev._subEvents.IsValueCreated)
                    foreach (Event e in ev.GetSubEvents())
                        _saveLoadedTree(e);
                if (ev._next.IsValueCreated)
                    ev = ev._next.Value;
                else
                    break;
            }
        }

        public IEvent GetSuccessor()
        {
            lock (_engine.RundownSync)
            {
                return InternalGetSuccessor();
            }
        }

        internal Event InternalGetSuccessor()
        {
            var next = _getSuccessor();
            while (next != null && next.Length.Equals(TimeSpan.Zero))
            {
                var current = next;
                next = current._getSuccessor();
            }
            return next;
        }

        internal Event FindVisibleSubEvent()
        {
            if (_eventType != TEventType.Rundown)
                throw new InvalidOperationException("FindVisibleSubEvent: EventType is not Rundown");
            var se = GetSubEvents().FirstOrDefault(e => ((e.EventType == TEventType.Live || e.EventType == TEventType.Movie) && e.Layer == VideoLayer.Program) || e.EventType == TEventType.Rundown) as Event;
            if (se != null && se.EventType == TEventType.Rundown)
                return se.FindVisibleSubEvent();
            return se;
        }

        internal long MediaSeek
        {
            get
            {
                if (ServerMediaPRI != null)
                {
                    long seek = (ScheduledTc.Ticks - ServerMediaPRI.TcStart.Ticks) / Engine.FrameTicks;
                    return seek < 0 ? 0 : seek;
                }
                return 0;
            }
        }

        internal bool IsFinished()
        {
            return _position >= _duration.Ticks / Engine.FrameTicks;
        }

        internal long TransitionInFrames()
        {
            return TransitionTime.Ticks / Engine.FrameTicks;
        }

        protected override bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!base.SetField(ref field, value, propertyName))
                return false;
            IsModified = true;
            return true;
        }

        private void _delete()
        {
            _remove();
            IsDeleted = true;
            DatabaseProvider.Database.DeleteEvent(this);
            _engine.NotifyEventDeleted(this);
            IsModified = false;
        }

        private void _setPlayState(TPlayState newPlayState)
        {
            if (!SetField(ref _playState, newPlayState, nameof(PlayState)))
                return;
            switch (newPlayState)
            {
                case TPlayState.Playing:
                    StartTime = Engine.CurrentTime;
                    StartTc = ScheduledTc + TimeSpan.FromTicks(_position * Engine.FrameTicks);
                    break;
                case TPlayState.Scheduled:
                    StartTime = default(DateTime);
                    StartTc = ScheduledTc;
                    Position = 0;
                    _updateScheduledTimeWithSuccessors();
                    break;
                case TPlayState.Paused:
                    Position = 0;
                    break;
                case TPlayState.Played:
                    _updateMediaLastPlayedTime();
                    break;
            }
        }

        private void _updateMediaLastPlayedTime()
        {
            var media = this.Media as ServerMedia;
            if (media is null)
                return;
            Task.Run(() =>
            {
                media.LastPlayed = this.StartTime;
                media.Save();
            });
        }

        private Event _getVisualParent()
        {
            Event curr = this;
            Event prior = curr._prior.Value;
            while (prior != null)
            {
                curr = prior;
                prior = curr._prior.Value;
            }
            return curr._parent.Value;
        }

        private Event _findLast()
        {
            Event curr = this;
            Event next = curr._next.Value;
            while (next != null)
            {
                curr = next;
                next = curr._next.Value;
            }
            return curr;
        }

        private Event _getPredecessor()
        {
            Event predecessor = _prior.Value ?? _parent.Value?._prior.Value;
            Event nextLevel = predecessor;
            while (nextLevel != null)
                if (nextLevel._eventType == TEventType.Rundown)
                {
                    nextLevel = (Event)predecessor._subEvents.Value.FirstOrDefault();
                    if (nextLevel != null)
                    {
                        nextLevel = nextLevel._findLast();
                        predecessor = nextLevel;
                    }
                }
                else
                    nextLevel = null;
            return predecessor;
        }

        private Event _getSuccessor()
        {
            var eventType = _eventType;
            if (eventType == TEventType.Movie || eventType == TEventType.Live || eventType == TEventType.Rundown)
                return _next.Value ?? _getVisualParent()?._getSuccessor();
            return null;
        }

        private TimeSpan _computedDuration()
        {
            if (_eventType == TEventType.Rundown)
            {
                long maxlen = 0;
                foreach (Event e in _subEvents.Value)
                {
                    var n = e;
                    long len = 0;
                    while (n != null)
                    {
                        len += n.Length.Ticks;
                        n = n._next.Value;
                        if (n != null) // first item's transition time doesn't count
                            len -= n.IsEnabled ? n.TransitionTime.Ticks : 0;
                    }
                    if (len > maxlen)
                        maxlen = len;
                }
                return ((Engine)Engine).AlignTimeSpan(TimeSpan.FromTicks(maxlen));
            }
            else
                return _duration;
        }

        private void _durationChanged()
        {
            NotifyPropertyChanged(nameof(EndTime));
            if (!(_next.Value is null))
            {
                _next.Value._uppdateScheduledTime();
                foreach (Event e in _next.Value.GetSubAndNextEvents())
                    e._uppdateScheduledTime();
            }
            Event owner = _getVisualParent();
            if (owner != null && owner._eventType == TEventType.Rundown)
                owner.Duration = owner._computedDuration();
        }

        private bool _uppdateScheduledTime()
        {
            Event baseEvent;
            DateTime determinedTime = default;
            switch (StartType)
            {
                case TStartType.After:
                    baseEvent = _getPredecessor();
                    if (baseEvent != null)
                        determinedTime = ((Engine)Engine).AlignDateTime(baseEvent.EndTime - _transitionTime);
                    break;
                case TStartType.WithParent:
                    baseEvent = _parent.Value;
                    if (baseEvent != null)
                        determinedTime = ((Engine)Engine).AlignDateTime(baseEvent.ScheduledTime + _scheduledDelay);
                    break;
                case TStartType.WithParentFromEnd:
                    baseEvent = _parent.Value;
                    if (baseEvent != null)
                        determinedTime = ((Engine)Engine).AlignDateTime(baseEvent.EndTime - _scheduledDelay - _duration);
                    break;
                default:
                    return false;
            }
            if (determinedTime != default && SetField(ref _scheduledTime, determinedTime, nameof(ScheduledTime)))
            {
                NotifyPropertyChanged(nameof(Offset));
                NotifyPropertyChanged(nameof(EndTime));
                return true;
            }
            return false;
        }

        private void _subEventsRemove(Event subEventToRemove)
        {
            if (!_subEvents.Value.Remove(subEventToRemove))
                return;
            if (_eventType == TEventType.Rundown)
                Duration = _computedDuration();
            NotifySubEventChanged(subEventToRemove, CollectionOperation.Remove);
            NotifyPropertyChanged(nameof(SubEventsCount));
        }

        private void _updateScheduledTimeWithSuccessors()
        {
            if (_uppdateScheduledTime())
                foreach (Event e in GetSuccessors())
                    if (!e._uppdateScheduledTime())
                        break;
        }

        private IEnumerable<IEvent> GetSubAndNextEvents()
        {
            lock(_engine.RundownSync)
            {
                return _getSubAndNextEvents();
            }
        }

        private IEnumerable<IEvent> _getSubAndNextEvents()
        {
            var ev = this;
            while (!(ev is null))
            {
                foreach (Event se in ev.GetSubEvents())
                {
                    yield return se;
                    foreach (var s in se._getSubAndNextEvents())
                        yield return s;
                }
                ev = ev._next.Value;
                if (!(ev is null))
                    yield return ev;
            }
        }


        private IEnumerable<Event> GetSuccessors()
        {
            foreach (Event ev in GetSubAndNextEvents())
                yield return ev;
            Event current = this;
            while (!((current = current._getVisualParent()) is null))
            {
                if (current._next.Value is null)
                    continue;
                yield return current._next.Value;
                foreach (Event ev in current._next.Value.GetSubAndNextEvents())
                    yield return ev;
            }
            yield break;
        }

        private void _setScheduledTime(DateTime time)
        {
            if (SetField(ref _scheduledTime, time, nameof(ScheduledTime)))
            {
                NotifyPropertyChanged(nameof(Offset));
                NotifyPropertyChanged(nameof(EndTime));
                foreach (Event e in GetSuccessors())
                    if (!e._uppdateScheduledTime())
                        break;
            }
        }

        private void NotifySubEventChanged(Event e, CollectionOperation operation)
        {
            SubEventChanged?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, operation));
        }

        private void AclEvent_Saved(object sender, EventArgs e)
        {
            lock(_engine.RundownSync)
            {
                _aclEvent_Saved();
            }
        }

        private void _aclEvent_Saved()
        {
            NotifyPropertyChanged(nameof(CurrentUserRights));
            if (!_subEvents.IsValueCreated)
                return;
            foreach (Event subEvent in GetSubEvents())
            {
                subEvent._aclEvent_Saved();
                var current = subEvent._next;
                while (current?.IsValueCreated == true)
                {
                    current.Value?._aclEvent_Saved();
                    current = current.Value?._next;
                }
            }
        }

        private void NotifyLocated()
        {
            NotifyPropertyChanged(nameof(CurrentUserRights));
            _engine.NotifyEventLocated(this);
        }

        internal void NotifyMediaVerified(IMedia media)
        {
            NotifyPropertyChanged(nameof(Media));
        }

    }

}
