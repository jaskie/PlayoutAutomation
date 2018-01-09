//#undef DEBUG
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using TAS.Common;
using TAS.Remoting.Server;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using TAS.Database;
using TAS.Common.Interfaces;
using TAS.Server.Media;
using TAS.Server.Security;

namespace TAS.Server
{
    [DebuggerDisplay("{" + nameof(_eventName) + "}")]
    public class Event : DtoBase, IEventPesistent
    {
        bool _isForcedNext;

        private bool _isModified;

        TPlayState _playState;

        private long _position;

        [JsonProperty(nameof(IEventPesistent.Engine))]
        private readonly Engine _engine;

        private readonly Lazy<SynchronizedCollection<Event>> _subEvents;

        private Lazy<PersistentMedia> _serverMediaPRI;

        private Lazy<PersistentMedia> _serverMediaSEC;

        private Lazy<PersistentMedia> _serverMediaPRV;

        private Lazy<Event> _parent;

        private Lazy<Event> _prior;

        private Lazy<Event> _next;

        private Lazy<List<IAclRight>> _rights;

        private bool _isDeleted;

        private bool _isCGEnabled;

        private byte _crawl;

        private byte _logo;

        private byte _parental;

        private ulong _idEventBinding;

        private double? _audioVolume;

        private TimeSpan _duration;

        private bool _isEnabled = true;

        private TEventType _eventType;

        private bool _isHold;

        private bool _isLoop;

        private string _idAux;

        private ulong _idProgramme;

        private VideoLayer _layer = VideoLayer.None;

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
        
        static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(Event));

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
                    byte parental)
        {
            _engine = engine;
            Id = idRundownEvent;
            _idEventBinding = idEventBinding;
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
            _setMedia(null, mediaGuid);
             _subEvents = new Lazy<SynchronizedCollection<Event>>(() =>
             {
                 var result = new SynchronizedCollection<Event>();
                 if (Id != 0)
                 {
                     var seList = Engine.DbReadSubEvents(this);
                     foreach (Event e in seList)
                     {
                         e.Parent = this;
                         result.Add(e);
                     }
                 }
                 return result;
             });

            _next = new Lazy<Event>(() =>
            {
                var next = (Event)Engine.DbReadNext(this);
                if (next != null)
                    next.Prior = this;
                return next;
            });

            _prior = new Lazy<Event>(() =>
            {
                Event prior = null;
                if (startType == TStartType.After && _idEventBinding > 0)
                    prior = (Event)Engine.DbReadEvent(_idEventBinding);
                if (prior != null)
                    prior.Next = this;
                return prior;
            });

            _parent = new Lazy<Event>(() =>
            {
                if ((startType == TStartType.WithParent || startType == TStartType.WithParentFromEnd) && _idEventBinding > 0)
                    return (Event)Engine.DbReadEvent(_idEventBinding);
                return null;
            });

            _rights = new Lazy<List<IAclRight>>(() => Db.DbReadEventAclList<EventAclRight>(this, _engine.AuthenticationService as IAuthenticationServicePersitency));
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
        [JsonProperty]
        public ulong Id {get; set; }

        public ulong IdEventBinding => _idEventBinding;

        #endregion

        #region IEventProperties

        [JsonProperty]
        public double? AudioVolume
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        [JsonProperty]
        public TimeSpan Duration
        {
            get => _duration;
            set => _setDuration(((Engine)Engine).AlignTimeSpan(value));
        }

        [JsonProperty]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetField(ref _isEnabled, value))
                {
                    if (value)
                        _uppdateScheduledTime();
                    _durationChanged();
                }
            }
        }

        string _eventName;
        [JsonProperty]
        public string EventName
        {
            get => _eventName;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _eventName, value);
            }
        }

        [JsonProperty]
        public TEventType EventType
        {
            get => _eventType;
            set
            {
                if (SetField(ref _eventType, value))
                    if (value == TEventType.Live || value == TEventType.Rundown)
                    {
                        _serverMediaPRI = null;
                        _serverMediaSEC = null;
                        _serverMediaPRV = null;
                    }
            }
        }

        [JsonProperty]
        public bool IsHold
        {
            get => _isHold;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _isHold, value);
            }
        }

        [JsonProperty]
        public bool IsLoop
        {
            get => _isLoop;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _isLoop, value);
            }
        }

        [JsonProperty]
        public string IdAux
        {
            get => _idAux;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _idAux, value);
            }
        }

        [JsonProperty]
        public ulong IdProgramme
        {
            get => _idProgramme;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _idProgramme, value);
            }
        }

        [JsonProperty]
        public VideoLayer Layer { get { return _layer; } set { SetField(ref _layer, value); } }

        [JsonProperty]
        public TimeSpan? RequestedStartTime
        {
            get => _requestedStartTime;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                if (SetField(ref _requestedStartTime, value))
                    NotifyPropertyChanged(nameof(Offset)); 
            }
        }

        [JsonProperty]
        public TimeSpan ScheduledDelay
        {
            get => _scheduledDelay;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _scheduledDelay, ((Engine) Engine).AlignTimeSpan(value));
            }
        }

        [JsonProperty]
        public TimeSpan ScheduledTc
        {
            get => _scheduledTc;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _scheduledTc, ((Engine) Engine).AlignTimeSpan(value));
            }
        }

        [JsonProperty]
        public DateTime ScheduledTime
        {
            get => _scheduledTime;
            set
            {
                if (_startType == TStartType.Manual || _startType == TStartType.OnFixedTime && _playState == TPlayState.Scheduled && HaveRight(EventRight.Modify))
                    _setScheduledTime(((Engine)Engine).AlignDateTime(value));
            }
        }

        [JsonProperty]
        public DateTime StartTime
        {
            get => _startTime;
            internal set
            {
                if (SetField(ref _startTime, value))
                {
                    if (value != default(DateTime))
                        _setScheduledTime(value);
                }
            }
        }

        [JsonProperty]
        public TStartType StartType
        {
            get => _startType;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
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

        [JsonProperty]
        public TimeSpan TransitionTime
        {
            get => _transitionTime;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                if (SetField(ref _transitionTime, ((Engine)Engine).AlignTimeSpan(value)))
                {
                    _uppdateScheduledTime();
                    _durationChanged();
                }
            }
        }

        [JsonProperty]
        public TimeSpan TransitionPauseTime
        {
            get => _transitionPauseTime;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _transitionPauseTime, ((Engine)Engine).AlignTimeSpan(value));
            }
        }

        [JsonProperty]
        public TTransitionType TransitionType
        {
            get => _transitionType;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _transitionType, value);
            }
        }

        [JsonProperty]
        public TEasing TransitionEasing
        {
            get => _transitionEasing;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _transitionEasing, value);
            }
        }

        [JsonProperty]
        public AutoStartFlags AutoStartFlags
        {
            get => _autoStartFlags;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _autoStartFlags, value);
            }
        }

        [JsonProperty]
        public Guid MediaGuid
        {
            get => _mediaGuid;
            set => _setMedia(null, value);
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
            }
            return right;
        }

        public bool DeleteRight(IAclRight item)
        {
            var right = (AclRightBase)item;
            lock (_rights)
            {
                var success = _rights.Value.Remove(right);
                if (success)
                    right.Delete();
                return success;
            }
        }

        #endregion // IAclObject

        [JsonProperty]
        public bool IsForcedNext
        {
            get => _isForcedNext;
            internal set => SetField(ref _isForcedNext, value);
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    NotifyPropertyChanged(nameof(IsModified));
                }
            }
        }

        [JsonProperty]
        public virtual TPlayState PlayState
        {
            get => _playState;
            set => _setPlayState(value);
        }

        [JsonProperty]
        public long Position // in frames
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    PositionChanged?.Invoke(this, new EventPositionEventArgs(value, _duration - TimeSpan.FromTicks(Engine.FrameTicks * value)));
                }
            }
        }

        public IEnumerable<IEvent> SubEvents { get { lock (_subEvents) return _subEvents.Value.ToArray(); } }

        [JsonProperty]
        public int SubEventsCount => _subEvents.Value.Count;

        public IEngine Engine => _engine;

        [JsonProperty]
        public TimeSpan Length => _isEnabled ? _duration : TimeSpan.Zero;

        [JsonProperty]
        public DateTime EndTime => _scheduledTime + Length;

        [JsonProperty]
        public TimeSpan StartTc
        {
            get => _startTc;
            set
            {
                value = ((Engine)Engine).AlignTimeSpan(value);
                SetField(ref _startTc, value);
            }
        }

        [JsonProperty]
        public IMedia Media
        {
            get => ServerMediaPRI;
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                var newMedia = value as PersistentMedia;
                _setMedia(newMedia, newMedia?.MediaGuid ?? Guid.Empty);
            }
        }

        public IEvent Parent
        {
            get => _parent.Value;
            private set            {
                if (_parent.IsValueCreated || value != _parent.Value)
                {
                    _parent = new Lazy<Event>(() => (Event)value);
                    NotifyPropertyChanged(nameof(Parent));
                }
            }
        }

        public IEvent Prior
        {
            get => _prior.Value;
            private set
            {
                if (!_prior.IsValueCreated || value != _prior.Value)
                {
                    _prior = new Lazy<Event>(() => (Event)value);
                    NotifyPropertyChanged(nameof(Prior));
                }
            }
        }

        public IEvent Next
        {
            get => _next.Value;
            private set
            {
                if (!_next.IsValueCreated || value != _next.Value)
                {
                    _next = new Lazy<Event>(() => (Event)value);
                    NotifyPropertyChanged(nameof(Next));
                    if (value != null)
                        IsLoop = false;
                }
            }
        }

        [JsonProperty]
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

        [JsonProperty]
        public bool IsDeleted => _isDeleted;

        [JsonProperty]
        public bool IsCGEnabled
        {
            get { return _isCGEnabled; }
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _isCGEnabled, value);
            }
        }

        [JsonProperty]
        public byte Crawl
        {
            get { return _crawl; }
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _crawl, value);
            }
        }

        [JsonProperty]
        public byte Logo
        {
            get { return _logo; }
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _logo, value);
            }
        }

        [JsonProperty]
        public byte Parental
        {
            get { return _parental; }
            set
            {
                if (!HaveRight(EventRight.Modify))
                    return;
                SetField(ref _parental, value);
            }
        }

        public event EventHandler<EventPositionEventArgs> PositionChanged;

        public event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;

        public void Remove()
        {
            Event next;
            lock (_engine.RundownSync)
            {
                Event parent = Parent as Event;
                next = Next as Event;
                Event prior = Prior as Event;
                TStartType startType = _startType;
                _engine.RemoveRootEvent(this);
                if (next != null)
                {
                    next.Parent = parent;
                    next.Prior = prior;
                    next.StartType = startType;
                    if (prior == null)
                        next._uppdateScheduledTime();
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
                    prior.Next = next;
                    prior._durationChanged();
                }
            }
            next?.Save();
            Next = null;
            Prior = null;
            Parent = null;
            _idEventBinding = 0;
            StartType = TStartType.None;
        }

        public bool MoveUp()
        {
            if (!HaveRight(EventRight.Modify))
                return false;
            Event e2;
            Event e4;
            lock (_engine.RundownSync)
            {
                // this = e3
                e2 = Prior as Event;
                e4 = Next as Event; // load if nescessary
                Debug.Assert(e2 != null, "Cannot move up - it's the first event");
                if (e2 == null)
                    return false;
                Event e2Parent = e2.Parent as Event;
                Event e2Prior = e2.Prior as Event;
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
                    e2Prior.Next = this;
                StartType = e2._startType;
                AutoStartFlags = e2.AutoStartFlags;
                Prior = e2Prior;
                Parent = e2Parent;
                _idEventBinding = e2._idEventBinding;
                e2.Prior = this;
                e2.StartType = TStartType.After;
                e2.Next = e4;
                e2.Parent = null;
                Next = e2;
                if (e4 != null)
                    e4.Prior = e2;
            }
            _uppdateScheduledTime();
            e4?.Save();
            e2.Save();
            Save();
            NotifyLocated();
            return true;
        }

        public bool MoveDown()
        {
            if (!HaveRight(EventRight.Modify))
                return false;
            Event e3;
            Event e4;
            lock (_engine.RundownSync)
            {
                // this = e2
                e3 = Next as Event; // load if nescessary
                Debug.Assert(e3 != null, "Cannot move down - it's the last event");
                if (e3 == null)
                    return false;
                e4 = e3.Next as Event;
                Event e2Parent = Parent as Event;
                Event e2Prior = Prior as Event;
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
                    e2Prior.Next = e3;
                e3.StartType = _startType;
                e3.AutoStartFlags = _autoStartFlags;
                e3.Prior = e2Prior;
                e3.Parent = e2Parent;
                e3._idEventBinding = _idEventBinding;
                StartType = TStartType.After;
                e3.Next = this;
                Parent = null;
                Next = e4;
                Prior = e3;
                if (e4 != null)
                    e4.Prior = this;
            }
            e3._uppdateScheduledTime();
            e4?.Save();
            Save();
            e3.Save();
            NotifyLocated();
            return true;
        }

        public bool InsertAfter(IEvent e)
        {
            if (!HaveRight(EventRight.Create))
                return false;
            Event eventToInsert = (Event) e;
            Event next;
            lock (_engine.RundownSync)
            {
                Event oldParent = eventToInsert.Parent as Event;
                Event oldPrior = eventToInsert.Prior as Event;
                oldParent?._subEventsRemove(eventToInsert);
                _engine.RemoveRootEvent(eventToInsert);
                if (oldPrior != null)
                    oldPrior.Next = null;

                next = this.Next as Event;
                if (next == eventToInsert)
                    return false;
                this.Next = eventToInsert;
                eventToInsert.StartType = TStartType.After;
                eventToInsert.Prior = this;

                // notify about relocation
                eventToInsert.NotifyLocated();
                eventToInsert.Next = next;

                if (next != null)
                    next.Prior = eventToInsert;
            }
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
            if (!HaveRight(EventRight.Create))
                return false;
            Event eventToInsert = (Event) e;
            lock (_engine.RundownSync)
            {
                Event prior = this.Prior as Event;
                Event parent = this.Parent as Event;
                Event oldParent = eventToInsert.Parent as Event;
                Event oldPrior = eventToInsert.Prior as Event;
                oldParent?._subEventsRemove(eventToInsert);
                _engine.RemoveRootEvent(eventToInsert);
                if (oldPrior != null)
                    oldPrior.Next = null;

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
                        Parent = null;
                    }
                }
                eventToInsert.Parent = parent;
                eventToInsert.Prior = prior;

                if (prior != null)
                    prior.Next = eventToInsert;

                // notify about relocation
                eventToInsert.NotifyLocated();
                this.Prior = eventToInsert;
                eventToInsert.Next = this;
                this.StartType = TStartType.After;
            }
            // time calculations
            eventToInsert._uppdateScheduledTime();
            eventToInsert._durationChanged();

            eventToInsert.Save();
            Save();
            return true;
        }

        public bool InsertUnder(IEvent se, bool fromEnd)
        {
            if (!HaveRight(EventRight.Create))
                return false;
            Event subEventToAdd = (Event) se;
            lock (_engine.RundownSync)
            {
                Event oldPrior = subEventToAdd.Prior as Event;
                Event oldParent = subEventToAdd.Parent as Event;
                oldParent?._subEventsRemove(subEventToAdd);
                _engine.RemoveRootEvent(subEventToAdd);
                if (oldPrior != null)
                    oldPrior.Next = null;
                if (EventType == TEventType.Container)
                {
                    if (!(subEventToAdd.StartType == TStartType.Manual ||
                          subEventToAdd.StartType == TStartType.OnFixedTime)) // do not change if valid
                        subEventToAdd.StartType = TStartType.Manual;
                }
                else
                    subEventToAdd.StartType = fromEnd ? TStartType.WithParentFromEnd : TStartType.WithParent;
                subEventToAdd.Parent = this;
                subEventToAdd.IsHold = false;
                lock (_subEvents)
                {
                    _subEvents.Value.Add(subEventToAdd);
                }
                NotifyPropertyChanged(nameof(SubEventsCount));
                NotifySubEventChanged(subEventToAdd, CollectionOperation.Add);
                if (_eventType == TEventType.Rundown)
                    Duration = _computedDuration();
                Event prior = subEventToAdd.Prior as Event;
                if (prior != null)
                {
                    prior.Next = null;
                    subEventToAdd.Prior = null;
                    prior._durationChanged();
                }
            }
            subEventToAdd._uppdateScheduledTime();
            // notify about relocation
            subEventToAdd.NotifyLocated();
            Event lastToInsert = subEventToAdd.Next as Event;
            while (lastToInsert != null)
            {
                lastToInsert.NotifyLocated();
                lastToInsert = lastToInsert.Next as Event;
            }
            if (_idEventBinding == 0)
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
                Event ev = SubEvents.FirstOrDefault(e => e.EventType == TEventType.Movie || e.EventType == TEventType.Live || e.EventType == TEventType.Rundown) as Event;
                while (ev != null)
                {
                    TimeSpan? pt = ev.GetAttentionTime();
                    if (pt.HasValue)
                        return pauseTime + pt.Value;
                    pauseTime += ev.Length - ev.TransitionTime;
                    ev = ev.Next as Event;
                }
            }
            return null;
        }
        
        public void Delete()
        {
            if (!IsDeleted 
                && HaveRight(EventRight.Delete)
                && AllowDelete())
                _delete();
        }

        public MediaDeleteResult CheckCanDeleteMedia(IServerMedia media)
        {
            Event nev = this;
            while (nev != null)
            {
                if (nev.EventType == TEventType.Movie
                    && nev.Media == media
                    && nev.ScheduledTime >= Engine.CurrentTime)
                    return new MediaDeleteResult() { Result = MediaDeleteResult.MediaDeleteResultEnum.InFutureSchedule, Event = nev, Media = media };
                lock (nev._subEvents)
                {
                    foreach (Event se in nev._subEvents.Value.ToList())
                    {
                        MediaDeleteResult reason = se.CheckCanDeleteMedia(media);
                        if (reason.Result != MediaDeleteResult.MediaDeleteResultEnum.Success)
                            return reason;
                    }
                }
                nev = nev.Next as Event;
            }
            return MediaDeleteResult.NoDeny;
        }

        public void Save()
        {
            switch (_startType)
            {
                case TStartType.After:
                    _idEventBinding = Prior?.Id ?? 0;
                    break;
                default:
                    _idEventBinding = Parent?.Id ?? 0;
                    break;
            }
            try
            {
                if (Id == 0)
                    this.DbInsertEvent();
                else
                    this.DbUpdateEvent();
                IsModified = false;
                Debug.WriteLine(this, "Event saved");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception saving event {0}", EventName);
            }
        }

        public bool AllowDelete()
        {
            if ((_playState == TPlayState.Fading || _playState == TPlayState.Paused || _playState == TPlayState.Playing) &&
                (_eventType == TEventType.Live || _eventType == TEventType.Movie || _eventType == TEventType.Rundown))
                return false;
            if (_eventType == TEventType.Container && SubEvents.Any())
                return false;
            foreach (var se in SubEvents)
            {
                IEvent ne = se;
                while (ne != null)
                {
                    if (!ne.AllowDelete())
                        return false;
                    ne = ne.Next;
                }
            }
            return true;
        }

        public bool HaveRight(EventRight right)
        {
            if (_engine.HaveRight(EngineRight.Rundown))
                return true;
            return (EffectiveRights() & (ulong)right) > 0;
        }
        
        public override string ToString()
        {
            return $"{EventType} {EventName}";
        }

        internal PersistentMedia ServerMediaPRI => _serverMediaPRI?.Value;

        internal PersistentMedia ServerMediaSEC => _serverMediaSEC?.Value;

        internal PersistentMedia ServerMediaPRV => _serverMediaPRV?.Value;

        internal void SaveLoadedTree()
        {
            if (IsModified && _engine != null)
                Save();
            lock (_subEvents)
            {
                if (_subEvents != null && _subEvents.IsValueCreated && _subEvents.Value != null)
                {
                    foreach (Event e in _subEvents.Value)
                    {
                        Event ce = e;
                        do
                        {
                            ce.SaveLoadedTree();
                            var lne = ce._next;
                            if (lne != null && lne.IsValueCreated)
                                ce = lne.Value;
                            else
                                ce = null;
                        } while (ce != null);
                    }
                }
            }
        }

        internal Event GetEnabledSuccessor()
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
            var se = SubEvents.FirstOrDefault(e => ((e.EventType == TEventType.Live || e.EventType == TEventType.Movie) && e.Layer == VideoLayer.Program) || e.EventType == TEventType.Rundown) as Event;
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
        
        protected override void DoDispose()
        {
            var media = _serverMediaPRI;
            if (media != null && media.IsValueCreated && media.Value != null)
                media.Value.PropertyChanged -= _serverMediaPRI_PropertyChanged;
            _serverMediaPRI = null;
            _serverMediaSEC = null;
            _serverMediaPRV = null;
            base.DoDispose();
        }

        protected override bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (base.SetField(ref field, value, propertyName))
            {
                IsModified = true;
                return true;
            }
            return false;
        }


        private void _delete()
        {
            foreach (var se in SubEvents)
            {
                var ne = se as Event;
                while (ne != null)
                {
                    var next = ne.Next as Event;
                    ne._delete();
                    ne = next;
                }
                (se as Event)?._delete();
            }
            Remove();
            _isDeleted = true;
            this.DbDeleteEvent();
            _engine.RemoveEvent(this);
            _engine.NotifyEventDeleted(this);
            _isModified = false;
            Dispose();
        }

        private void _setPlayState(TPlayState newPlayState)
        {
            if (SetField(ref _playState, newPlayState, nameof(PlayState)))
            {
                if (newPlayState == TPlayState.Playing)
                {
                    StartTime = Engine.CurrentTime;
                    StartTc = ScheduledTc + TimeSpan.FromTicks(_position * Engine.FrameTicks);
                }
                if (newPlayState == TPlayState.Scheduled)
                {
                    StartTime = default(DateTime);
                    StartTc = ScheduledTc;
                    Position = 0;
                    _uppdateScheduledTime();
                }
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
            {
                Event result = _next.Value;
                if (result == null)
                    result = _getVisualParent()?._getSuccessor();
                return result;
            }
            return null;
        }

        private TimeSpan _computedDuration()
        {
            if (_eventType == TEventType.Rundown)
            {
                long maxlen = 0;
                foreach (var e in SubEvents)
                {
                    IEvent n = e;
                    long len = 0;
                    while (n != null)
                    {
                        len += n.Length.Ticks;
                        n = n.Next;
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
            if (_eventType == TEventType.Movie || _eventType == TEventType.Rundown || _eventType == TEventType.Live)
            {
                Event owner = _getVisualParent();
                if (owner != null && owner._eventType == TEventType.Rundown)
                    owner.Duration = owner._computedDuration();
                Event ev = _getSuccessor();
                if (ev != null)
                    ev._uppdateScheduledTime();
                NotifyPropertyChanged(nameof(EndTime));
            }
        }

        private void _uppdateScheduledTime()
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
                    return;
            }
            if (determinedTime != DateTime.MinValue)
                _setScheduledTime(determinedTime);
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

        private void _serverMediaPRI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioVolume) && AudioVolume == null)
                NotifyPropertyChanged(nameof(AudioVolume));
        }

        private void _setMedia(PersistentMedia media, Guid mediaGuid)
        {
            if (mediaGuid == _mediaGuid)
                return; // do nothing if same mediaGuid is assigned
            var serverMediaPRI = _serverMediaPRI;
            if (serverMediaPRI != null && serverMediaPRI.IsValueCreated && serverMediaPRI.Value != null)
            {
                if (media == serverMediaPRI.Value)
                    return; // do nothing if the same media is assigned;
                serverMediaPRI.Value.PropertyChanged -= _serverMediaPRI_PropertyChanged;
            }
            if (mediaGuid != Guid.Empty)
            {
                _serverMediaPRI = new Lazy<PersistentMedia>(() =>
                {
                    var priMedia = media ?? _getMediaFromDir(mediaGuid, _eventType == TEventType.Animation ? (MediaDirectory)Engine.MediaManager.AnimationDirectoryPRI : (MediaDirectory)Engine.MediaManager.MediaDirectoryPRI);
                    if (priMedia != null)
                        priMedia.PropertyChanged += _serverMediaPRI_PropertyChanged;
                    return priMedia;
                });
                if (media != null)
                    media = _serverMediaPRI.Value; // only to imediately read lazy's value

                _serverMediaSEC = new Lazy<PersistentMedia>(() => _getMediaFromDir(mediaGuid, _eventType == TEventType.Animation ? (MediaDirectory)Engine.MediaManager.AnimationDirectorySEC : (MediaDirectory)Engine.MediaManager.MediaDirectorySEC));
                _serverMediaPRV = new Lazy<PersistentMedia>(() => _getMediaFromDir(mediaGuid, _eventType == TEventType.Animation ? (MediaDirectory)Engine.MediaManager.AnimationDirectoryPRV : (MediaDirectory)Engine.MediaManager.MediaDirectoryPRV));
            }
            _mediaGuid = mediaGuid;
            NotifyPropertyChanged(nameof(MediaGuid));
            NotifyPropertyChanged(nameof(Media));
        }

        private void _setDuration(TimeSpan newDuration)
        {
            var oldDuration = _duration;
            if (SetField(ref _duration, newDuration, nameof(Duration)))
            {
                if (_eventType == TEventType.Live || _eventType == TEventType.Movie)
                {
                    foreach (Event e in SubEvents.Where(ev => ev.EventType == TEventType.StillImage))
                    {
                        TimeSpan nd = e._duration + newDuration - oldDuration;
                        e._setDuration(nd > TimeSpan.Zero ? nd : TimeSpan.Zero);
                    }
                }
                _durationChanged();
            }

        }

        private void _setScheduledTime(DateTime time)
        {
            if (SetField(ref _scheduledTime, time, nameof(ScheduledTime)))
            {
                Debug.WriteLine($"Scheduled time updated: {this}");
                Event toUpdate = _getSuccessor();
                if (toUpdate != null)
                    toUpdate._uppdateScheduledTime();  // trigger update all next events
                lock (_subEvents)
                {
                    foreach (Event ev in _subEvents.Value) //update all sub-events
                        ev._uppdateScheduledTime();
                }
                NotifyPropertyChanged(nameof(Offset));
                NotifyPropertyChanged(nameof(EndTime));
            }
        }

        private void NotifySubEventChanged(Event e, CollectionOperation operation)
        {
            SubEventChanged?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, operation));
        }

        private PersistentMedia _getMediaFromDir(Guid mediaGuid, MediaDirectory dir)
        {
            if (dir != null)
            {
                var newMedia = dir.FindMediaByMediaGuid(mediaGuid);
                if (newMedia is PersistentMedia)
                    return (PersistentMedia)newMedia;
            }
            return null;
        }

        private ulong EffectiveRights()
        {
            if (!(Thread.CurrentPrincipal.Identity is IUser identity))
                return 0;
            if (identity.IsAdmin)
                return ulong.MaxValue; // Full rights
            var visualParent = _getVisualParent();
            ulong acl = visualParent?.EffectiveRights() ?? 0;
            var groups = identity.GetGroups();
            lock (_rights)
            {
                var userRights = _rights.Value.Where(r => r.SecurityObject == identity || groups.Any(g => g == r.SecurityObject));
                foreach (var right in userRights)
                    acl |= right.Acl;
            }
            return acl;
        }

        private void NotifyLocated()
        {
            _engine.NotifyEventLocated(this);
        }
       
    }

}
