﻿#undef DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using TAS.Common;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using System.Threading;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.Security;
using TAS.Server.Media;
using TAS.Server.Security;
using jNet.RPC;
using System.Threading.Tasks;

namespace TAS.Server
{
    [DebuggerDisplay("{" + nameof(_eventName) + "}")]
    public class Event : ServerObjectBase, IEventPersistent
    {
        bool _isForcedNext;
        private bool _isModified;
        TPlayState _playState;
        private long _position;
        
        [DtoMember(nameof(IEventPersistent.Engine))]
        private readonly Engine _engine;
        private readonly object _rundownSync;
        private readonly Lazy<SynchronizedCollection<Event>> _subEvents;
        private Lazy<Event> _parent;
        private Lazy<Event> _prior;
        private Lazy<Event> _next;
        private readonly Lazy<List<IAclRight>> _rights;
        private bool _isCGEnabled;
        private byte _crawl;
        private byte _logo;
        private byte _parental;
        private int _videoSwitchPort = -1;
        private RecordingInfo _recordingInfo;
        private double? _audioVolume;
        private TimeSpan _duration;
        private bool _isEnabled;
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

        private IVideoSwitchPort _inputPort;

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
                    short videoSwitchPort,
                    RecordingInfo recordingInfo)
        {
            _engine = engine;
            _rundownSync = engine.RundownSync;
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
            _subEvents = new Lazy<SynchronizedCollection<Event>>(() =>
            {
                var result = new SynchronizedCollection<Event>();
                if (Id == 0)
                    return result;
                var seList = DatabaseProvider.Database.ReadSubEvents(_engine, this);
                foreach (Event e in seList)
                {
                    e.SetParent(this);
                    result.Add(e);
                }
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
            _videoSwitchPort = videoSwitchPort;
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
        [XmlIgnore]
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
                if (_eventType == TEventType.Live || _eventType == TEventType.Movie)
                {
                    lock (_subEvents)
                    {
                        foreach (Event e in _subEvents.Value.Where(ev => ev.EventType == TEventType.StillImage))
                        {
                            var nd = e._duration + value - oldDuration;
                            e.Duration = nd > TimeSpan.Zero ? nd : TimeSpan.Zero;
                        }
                    }
                }
                _durationChanged();
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
                _durationChanged();
            }
        }

        string _eventName;        

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
                    _setScheduledTime(((Engine)Engine).AlignDateTime(value));
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
                    _setScheduledTime(value);
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
                    _uppdateScheduledTime();
                    _durationChanged();
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
            lock (_rights) return _rights.Value.AsReadOnly();
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

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (_isModified == value)
                    return;
                _isModified = value;
                NotifyPropertyChanged();
            }
        }

        [DtoMember]
        public virtual TPlayState PlayState
        {
            get => _playState;
            set => _setPlayState(value);
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

        public IEnumerable<IEvent> GetSubEvents() { lock (_subEvents) return _subEvents.Value.ToArray(); } 

        [DtoMember]
        public int SubEventsCount
        {
            get
            {
                lock (_subEvents) return _subEvents.Value.Count;
            }
        }

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

        private void SetParent(IEvent value) => _parent = new Lazy<Event>(() => (Event)value);

        public IEvent GetPrior() => _prior.Value;

        private void SetPrior(IEvent value) => _prior = new Lazy<Event>(() => (Event)value);

        public IEvent GetNext() => _next.Value;

        private void SetNext(IEvent value)
        {
            _next = new Lazy<Event>(() => (Event)value);
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
        public bool IsDeleted { get; private set; }

        [DtoMember]
        public bool IsCGEnabled
        {
            get => _isCGEnabled;
            set => SetField(ref _isCGEnabled, value);
        }

        [DtoMember]
        public IVideoSwitchPort InputPort
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
        public int VideoSwitchPort
        {
            get => _videoSwitchPort;
            set => SetField(ref _videoSwitchPort, value);
        }       

        public event EventHandler<EventPositionEventArgs> PositionChanged;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;

        public void Remove()
        {
            Event next;
            lock (_rundownSync)
            {
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
                        next.UpdateScheduledTimeWithSuccessors();
                }
                if (parent != null)
                {
                    lock (parent._subEvents)
                    {
                        parent._subEventsRemove(this);
                        if (next != null)
                        {
                            parent._subEvents.Value.Add(next);
                            parent.NotifyPropertyChanged(nameof(SubEventsCount));
                        }
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
            lock (_rundownSync)
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
                    lock (e2Parent._subEvents)
                    {
                        e2Parent._subEvents.Value.Remove(e2);
                        e2Parent.NotifySubEventChanged(e2, CollectionOperation.Remove);
                        e2Parent._subEvents.Value.Add(this);
                        e2Parent.NotifySubEventChanged(this, CollectionOperation.Add);
                    }
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
            }
            _uppdateScheduledTime();
            e4?.Save();
            e2._uppdateScheduledTime();
            e2.Save();
            Save();
            NotifyLocated();
            return true;
        }

        public bool MoveDown()
        {
            Event e3;
            Event e4;
            lock (_rundownSync)
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
                    lock (e2Parent._subEvents)
                    {
                        e2Parent._subEvents.Value.Remove(this);
                        e2Parent.NotifySubEventChanged(this, CollectionOperation.Remove);
                        e2Parent._subEvents.Value.Add(e3);
                        e2Parent.NotifySubEventChanged(e3, CollectionOperation.Add);
                    }
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
            }
            e3._uppdateScheduledTime();
            e4?.Save();
            _uppdateScheduledTime();
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
                _engine.RemoveRootEvent(eventToInsert);
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
            }
            // notify about relocation
            eventToInsert.NotifyLocated();
            
            //time calculations
            eventToInsert._uppdateScheduledTime();
            eventToInsert._durationChanged();

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
                _engine.RemoveRootEvent(eventToInsert);
                if (oldPrior != null)
                    oldPrior.SetNext(null);

                eventToInsert.StartType = _startType;
                if (prior == null)
                    eventToInsert.IsHold = false;

                if (parent != null)
                {
                    lock (parent._subEvents)
                    {
                        parent._subEvents.Value.Remove(this);
                        parent._subEvents.Value.Add(eventToInsert);
                        parent.NotifySubEventChanged(eventToInsert, CollectionOperation.Add);
                        SetParent(null);
                    }
                }
                eventToInsert.SetParent(parent);
                eventToInsert.SetPrior(prior);

                if (prior != null)
                    prior.SetNext(eventToInsert);

                this.SetPrior(eventToInsert);
                eventToInsert.SetNext(this);
                this.StartType = TStartType.After;
            }
            // notify about relocation
            eventToInsert.NotifyLocated();

            // time calculations
            eventToInsert._uppdateScheduledTime();
            eventToInsert._durationChanged();

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
                _engine.RemoveRootEvent(subEventToAdd);
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
                lock (_subEvents)
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
                    prior.UpdateScheduledTimeWithSuccessors();
                }
            }
            subEventToAdd.UpdateScheduledTimeWithSuccessors();
            
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
            if (!IsDeleted 
                && AllowDelete())
                _delete();
        }

        public MediaDeleteResult CheckCanDeleteMedia(IServerMedia media)
        {
            if (EventType == TEventType.Rundown && (PlayState == TPlayState.Played || EndTime < DateTime.Now))
                return MediaDeleteResult.NoDeny;
            var nev = this;
            while (nev != null)
            {
                if (nev.EventType == TEventType.Movie
                    && nev.Media == media
                    && nev.PlayState != TPlayState.Played)
                    return new MediaDeleteResult { Result = MediaDeleteResult.MediaDeleteResultEnum.InSchedule, Event = nev, Media = media };
                lock (nev._subEvents)
                {
                    foreach (var se in nev._subEvents.Value.ToList())
                    {
                        var reason = se.CheckCanDeleteMedia(media);
                        if (reason.Result != MediaDeleteResult.MediaDeleteResultEnum.Success)
                            return reason;
                    }
                }
                nev = nev.GetNext() as Event;
            }
            return MediaDeleteResult.NoDeny;
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
            foreach (var se in GetSubEvents())
            {
                IEvent ne = se;
                while (ne != null)
                {
                    if (!ne.AllowDelete())
                        return false;
                    ne = ne.GetNext();
                }
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

        internal static void SaveLoadedTree(Event ev)
        {
            Debug.Assert(!(ev._engine is null));
            while (!(ev is null))
            {
                if (ev.IsModified)
                    ev.Save();
                lock (ev._subEvents)
                {
                    if (!ev._subEvents.IsValueCreated || ev._subEvents.Value == null)
                        return;
                    foreach (var e in ev._subEvents.Value)
                        SaveLoadedTree(e);
                }
                if (ev._next.IsValueCreated)
                    ev = ev._next.Value;
                else
                    break;
            }
        }

        public IEvent GetSuccessor()
        {
            return InternalGetSuccessor();
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

        internal void SaveDelayed()
        {
            if (IsModified)
                Task.Run(Save);
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
            foreach (var se in GetSubEvents())
            {
                var ne = se as Event;
                while (ne != null)
                {
                    var next = ne.GetNext() as Event;
                    ne._delete();
                    ne = next;
                }
                (se as Event)?._delete();
            }
            Remove();
            IsDeleted = true;
            DatabaseProvider.Database.DeleteEvent(this);
            _engine.RemoveEvent(this);
            _engine.NotifyEventDeleted(this);
            _isModified = false;
            Dispose();
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
                    UpdateScheduledTimeWithSuccessors();
                    break;
                case TPlayState.Paused:
                    Position = 0;
                    break;
            }
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
                    lock (nextLevel._subEvents)
                        nextLevel = predecessor._subEvents.Value.FirstOrDefault();
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
                lock (_subEvents) 
                foreach (var e in _subEvents.Value)
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
                foreach (var e in GetSubAndNextEvents(_next.Value))
                {
                    e._uppdateScheduledTime();
                }
            }
            Event owner = _getVisualParent();
            if (owner != null && owner._eventType == TEventType.Rundown)
                owner.Duration = owner._computedDuration();
        }

        private bool _uppdateScheduledTime()
        {
            Event baseEvent;
            DateTime determinedTime = DateTime.MinValue;
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
            if (determinedTime != DateTime.MinValue && SetField(ref _scheduledTime, determinedTime, nameof(ScheduledTime)))
            {
                NotifyPropertyChanged(nameof(Offset));
                NotifyPropertyChanged(nameof(EndTime));
                return true;
            }
            return false;
        }

        private void _subEventsRemove(Event subEventToRemove)
        {
            lock (_subEvents)
            {
                if (_subEvents.Value.Remove(subEventToRemove))
                {
                    if (_eventType == TEventType.Rundown)
                        Duration = _computedDuration();
                    NotifySubEventChanged(subEventToRemove, CollectionOperation.Remove);
                    NotifyPropertyChanged(nameof(SubEventsCount));
                }
            }
        }


        private void UpdateScheduledTimeWithSuccessors()
        {
            if (_uppdateScheduledTime())
                foreach (Event e in GetSuccessors())
                    if (!e._uppdateScheduledTime())
                        break;
        }

        private static IEnumerable<Event> GetSubAndNextEvents(Event ev)
        {
            while (!(ev is null))
            {
                lock (ev._subEvents)
                {
                    foreach (var se in ev._subEvents.Value)
                    {
                        yield return se;
                        foreach (var s in GetSubAndNextEvents(se))
                            yield return s;
                    }
                }
                ev = ev._next.Value;
                if (!(ev is null))
                    yield return ev;
            }
        }

        private IEnumerable<Event> GetSuccessors()
        {
            foreach (var ev in GetSubAndNextEvents(this))
                yield return ev;
            Event current = this;
            while (!((current = current._getVisualParent()) is null))
            {
                if (current._next.Value is null)
                    continue;
                yield return current._next.Value;
                foreach (var ev in GetSubAndNextEvents(current._next.Value))
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
            NotifyPropertyChanged(nameof(CurrentUserRights));
            if (!_subEvents.IsValueCreated)
                return;
            foreach (var subEvent in _subEvents.Value)
            {
                subEvent.AclEvent_Saved(sender, e);
                var current = subEvent._next;
                while (current?.IsValueCreated == true)
                {
                    current.Value?.AclEvent_Saved(sender, e);
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
