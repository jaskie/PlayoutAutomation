#undef DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;
using TAS.Common;
using TAS.Server.Database;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Remoting.Server;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace TAS.Server
{
    [DebuggerDisplay("{_eventName}")]
    public class Event : DtoBase, IEventPesistent, IComparable
    {
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
                    decimal? audioVolume,
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
            _id = idRundownEvent;
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
                 if (_id != 0)
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
                return prior;
            });

            _parent = new Lazy<Event>(() =>
            {
                if ((startType == TStartType.WithParent || startType == TStartType.WithParentFromEnd) && _idEventBinding > 0)
                    return (Event)Engine.DbReadEvent(_idEventBinding);
                return null;
            });
        }
        #endregion //Constructor

#if DEBUG
        ~Event()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif

        static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(Event));

        #region IEventPesistent 
        private ulong _id = 0;
        [XmlIgnore]
        [JsonProperty]
        public ulong Id
        {
            get
            {
                if (_id == 0)
                    Save();
                return _id;
            }
            set { _id = value; }
        }
        ulong _idEventBinding;
        public ulong IdEventBinding { get { return _idEventBinding; } }
        #endregion

        #region IEventProperties

        decimal? _audioVolume;
        [JsonProperty]
        public decimal? AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, nameof(AudioVolume)); }
        }

        TimeSpan _duration;
        [JsonProperty]
        public TimeSpan Duration
        {
            get { return _duration; }
            set { _setDuration(((Engine)Engine).AlignTimeSpan(value)); }
        }

        private void _setDuration(TimeSpan newDuration)
        {
            if (SetField(ref _duration, newDuration, nameof(Duration)))
            {
                if (_eventType == TEventType.Live || _eventType == TEventType.Movie)
                {
                    foreach (Event e in SubEvents.Where(ev => ev.EventType == TEventType.StillImage))
                    {
                        TimeSpan nd = e._duration + newDuration - _duration;
                        e._setDuration(nd > TimeSpan.Zero ? nd : TimeSpan.Zero);
                    }
                }
                _durationChanged();
            }

        }

        bool _isEnabled = true;
        [JsonProperty]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (SetField(ref _isEnabled, value, nameof(IsEnabled)))
                    _durationChanged();
            }
        }

        string _eventName;
        [JsonProperty]
        public string EventName
        {
            get { return _eventName; }
            set { SetField(ref _eventName, value, nameof(EventName)); }
        }

        TEventType _eventType;
        [JsonProperty]
        public TEventType EventType
        {
            get { return _eventType; }
            set
            {
                if (SetField(ref _eventType, value, nameof(EventType)))
                    if (value == TEventType.Live || value == TEventType.Rundown)
                    {
                        _serverMediaPRI = null;
                        _serverMediaSEC = null;
                        _serverMediaPRV = null;
                    }
            }
        }

        bool _isHold;
        [JsonProperty]
        public bool IsHold { get { return _isHold; } set { SetField(ref _isHold, value, nameof(IsHold)); } }
        
        bool _isLoop;
        [JsonProperty]
        public bool IsLoop { get { return _isLoop; } set { SetField(ref _isLoop, value, nameof(IsLoop)); } }

        string _idAux; 
        [JsonProperty]
        public string IdAux { get { return _idAux; } set { SetField(ref _idAux, value, nameof(IdAux)); } }

        ulong _idProgramme;
        [JsonProperty]
        public ulong IdProgramme { get { return _idProgramme; } set { SetField(ref _idProgramme, value, nameof(IdProgramme)); } }

        VideoLayer _layer = VideoLayer.None;
        [JsonProperty]
        public VideoLayer Layer { get { return _layer; } set { SetField(ref _layer, value, nameof(Layer)); } }

        TimeSpan? _requestedStartTime;
        [JsonProperty]
        public TimeSpan? RequestedStartTime
        {
            get { return _requestedStartTime; }
            set
            {
                if (SetField(ref _requestedStartTime, value, nameof(RequestedStartTime)))
                    NotifyPropertyChanged(nameof(Offset)); 
            }
        }

        TimeSpan _scheduledDelay;
        [JsonProperty]
        public TimeSpan ScheduledDelay
        {
            get { return _scheduledDelay; }
            set { SetField(ref _scheduledDelay, ((Engine)Engine).AlignTimeSpan(value), nameof(ScheduledDelay)); }
        }

        TimeSpan _scheduledTc = TimeSpan.Zero;
        [JsonProperty]
        public TimeSpan ScheduledTc { get { return _scheduledTc; } set { SetField(ref _scheduledTc, ((Engine)Engine).AlignTimeSpan(value), nameof(ScheduledTc)); } }

        DateTime _scheduledTime;
        [JsonProperty]
        public DateTime ScheduledTime
        {
            get { return _scheduledTime; }
            set
            {
                if (_startType == TStartType.Manual || _startType == TStartType.OnFixedTime && _playState == TPlayState.Scheduled)
                    _setScheduledTime(((Engine)Engine).AlignDateTime(value));
            }
        }

        private void _setScheduledTime(DateTime time)
        {
            if (SetField(ref _scheduledTime, time, nameof(ScheduledTime)))
            {
                Debug.WriteLine($"Scheduled time updated: {this}");
                Event toUpdate = getSuccessor() ?? _getVisualParent()?._next.Value;
                if (toUpdate != null)
                    toUpdate._uppdateScheduledTime();  // trigger update all next events
                lock (_subEvents.Value.SyncRoot)
                {
                    foreach (Event ev in _subEvents.Value) //update all sub-events
                        ev._uppdateScheduledTime();
                }
                NotifyPropertyChanged(nameof(Offset));
                NotifyPropertyChanged(nameof(EndTime));
            }
        }

        DateTime _startTime;
        [JsonProperty]
        public DateTime StartTime
        {
            get { return _startTime; }
            internal set
            {
                if (SetField(ref _startTime, value, nameof(StartTime)))
                {
                    if (value != default(DateTime))
                        _setScheduledTime(value);
                }
            }
        }

        TStartType _startType;
        [JsonProperty]
        public TStartType StartType
        {
            get { return _startType; }
            set
            {
                var oldValue = _startType;
                if (SetField(ref _startType, value, nameof(StartType)))
                {
                    if (value == TStartType.OnFixedTime)
                        _engine.AddFixedTimeEvent(this);
                    if (oldValue == TStartType.OnFixedTime)
                        _engine.RemoveFixedTimeEvent(this);
                }
            }
        }

        TimeSpan _transitionTime;
        [JsonProperty]
        public TimeSpan TransitionTime
        {
            get { return _transitionTime; }
            set
            {
                if (SetField(ref _transitionTime, ((Engine)Engine).AlignTimeSpan(value), nameof(TransitionTime)))
                {
                    _uppdateScheduledTime();
                    _durationChanged();
                }
            }
        }

        TimeSpan _transitionPauseTime;
        [JsonProperty]
        public TimeSpan TransitionPauseTime
        {
            get { return _transitionPauseTime; }
            set { SetField(ref _transitionPauseTime, ((Engine)Engine).AlignTimeSpan(value), nameof(TransitionPauseTime)); }
        }

        TTransitionType _transitionType;
        [JsonProperty]
        public TTransitionType TransitionType
        {
            get { return _transitionType; }
            set { SetField(ref _transitionType, value, nameof(TransitionType)); }
        }

        TEasing _transitionEasing;
        [JsonProperty]
        public TEasing TransitionEasing
        {
            get { return _transitionEasing; }
            set { SetField(ref _transitionEasing, value, nameof(TransitionEasing)); }
        }

        AutoStartFlags _autoStartFlags;
        [JsonProperty]
        public AutoStartFlags AutoStartFlags { get { return _autoStartFlags; } set { SetField(ref _autoStartFlags, value, nameof(AutoStartFlags)); } }

        Guid _mediaGuid;
        [JsonProperty]
        public Guid MediaGuid
        {
            get { return _mediaGuid; }
            set
            {
                _setMedia(null, value);
            }
        }
        
        #endregion //IEventProperties



        bool _isForcedNext;
        [JsonProperty]
        public bool IsForcedNext { get { return _isForcedNext; } set { SetField(ref _isForcedNext, value, nameof(IsForcedNext)); } }

        private bool _isModified;
        public bool IsModified
        {
            get { return _isModified; }
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    NotifyPropertyChanged(nameof(IsModified));
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (object.Equals(obj, this))
                return 0;
            if (obj == null) return -1;
            int timecomp = this.ScheduledTime.CompareTo((obj as Event).ScheduledTime);
            timecomp = (timecomp == 0) ? this.ScheduledDelay.CompareTo((obj as Event).ScheduledDelay) : timecomp;
            return (timecomp == 0) ? this.Id.CompareTo((obj as Event).Id) : timecomp;
        }

        TPlayState _playState;
        [JsonProperty]
        public virtual TPlayState PlayState
        {
            get { return _playState; }
            set { _setPlayState(value); }
        }

        private bool _setPlayState(TPlayState newPlayState)
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
                    Event prev = _getPredecessor();
                    if (prev != null)
                        _setScheduledTime(prev.EndTime - _transitionTime);
                }
                return true;
            }
            return false;
        }

        private long _position = 0;
        public long Position // in frames
        {
            get { return _position; }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    PositionChanged?.Invoke(this, new EventPositionEventArgs(value, _duration - TimeSpan.FromTicks(Engine.FrameTicks * value)));
                }
            }
        }

        public bool IsFinished()
        {
            return _position >= _duration.Ticks / Engine.FrameTicks;
        }

        public event EventHandler<EventPositionEventArgs> PositionChanged;
        Lazy<SynchronizedCollection<Event>> _subEvents;
        public IList<IEvent> SubEvents { get { lock (_subEvents.Value.SyncRoot)  return _subEvents.Value.Cast<IEvent>().ToList(); } }

        public int SubEventsCount { get { return _subEvents.Value.Count; } }
        
        private readonly Engine _engine;
        public IEngine Engine { get { return _engine; } }

        
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
            Event predecessor = _prior.Value ?? _parent.Value;
            while (predecessor != null && predecessor.Length.Equals(TimeSpan.Zero))
                predecessor = predecessor._getPredecessor();
            Event nextLevel = predecessor;
            while (nextLevel != null)
                if (nextLevel._eventType == TEventType.Rundown && nextLevel._isEnabled)
                {
                    lock (nextLevel._subEvents.Value.SyncRoot)
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

        internal Event getSuccessor()
        {
            var eventType = _eventType;
            if (eventType == TEventType.Movie || eventType == TEventType.Live || eventType == TEventType.Rundown)
            {
                Event current = _next.Value;
                if (current != null)
                {
                    Event next = current._next.Value;
                    while (next != null && current.Length.Equals(TimeSpan.Zero))
                    {
                        current = next;
                        next = current._next.Value;
                    }
                }
                if (current == null)
                {
                    current = _getVisualParent();
                    if (current != null)
                        current = current.getSuccessor();
                }
                return current;
            }
            return null;
        }

        private void _uppdateScheduledTime()
        {
            DateTime nt = _scheduledTime; 
            Event baseEvent = null;
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
                
        [JsonProperty]
        public TimeSpan Length { get { return _isEnabled ? _duration : TimeSpan.Zero; } }

        [JsonProperty]
        public DateTime EndTime { get { return _scheduledTime + Length; } }

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
                NotifyPropertyChanged(nameof(EndTime));
                Event owner = _getVisualParent();
                if (owner != null && owner._eventType == TEventType.Rundown)
                    owner.Duration = owner._computedDuration();
                Event ev = getSuccessor() ?? owner?._next.Value;
                if (ev != null)
                    ev._uppdateScheduledTime();
            }
        }

        TimeSpan _startTc = TimeSpan.Zero;

        [JsonProperty]
        public TimeSpan StartTc
        {
            get
            {
                return _startTc;
            }
            set
            {
                value = ((Engine)Engine).AlignTimeSpan(value);
                SetField(ref _startTc, value, nameof(StartTc));
            }
        }

        [JsonProperty]
        public IMedia Media
        {
            get { return ServerMediaPRI; }
            set
            {
                var newMedia = value as PersistentMedia;
                _setMedia(newMedia, newMedia == null ? Guid.Empty: newMedia.MediaGuid);
            }
        }

        private void _serverMediaPRI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioVolume) && this.AudioVolume == null)
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
                        var priMedia = media != null ? media : _getMediaFromDir(mediaGuid, _eventType == TEventType.Animation ? (MediaDirectory)Engine.MediaManager.AnimationDirectoryPRI : (MediaDirectory)Engine.MediaManager.MediaDirectoryPRI);
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

        private Lazy<PersistentMedia> _serverMediaPRI;
        public PersistentMedia ServerMediaPRI
        {
            get
            {
                var l = _serverMediaPRI;
                if (l != null)
                    return l.Value;
                return null;
            }
        }

        private Lazy<PersistentMedia> _serverMediaSEC;
        public PersistentMedia ServerMediaSEC
        {
            get
            {
                var l = _serverMediaSEC;
                if (l != null)
                    return l.Value;
                return null;
            }
        }

        private Lazy<PersistentMedia> _serverMediaPRV;
        public PersistentMedia ServerMediaPRV
        {
            get
            {
                var l = _serverMediaPRV;
                if (l != null)
                    return l.Value;
                return null;
            }
        }

        public long MediaSeek
        {
            get
            {
                if (ServerMediaPRI != null)
                {
                    long seek = (this.ScheduledTc.Ticks - ServerMediaPRI.TcStart.Ticks) / Engine.FrameTicks;
                    return (seek < 0) ? 0 : seek;
                }
                return 0;
            }
        }


        Lazy<Event> _parent;
        public IEvent Parent
        {
            get { return _parent.Value; }
            protected set            {
                if (value != _parent.Value)
                {
                    Event v = value as Event;
                    _parent = new Lazy<Event>(() => v);
                    if (v != null)
                        _idEventBinding = v.Id;
                    NotifyPropertyChanged(nameof(Parent));
                }
            }
        }

        private Lazy<Event> _prior;
        public IEvent Prior
        {
            get { return _prior.Value; }
            protected set
            {
                if (value != _prior.Value)
                {
                    Event v = value as Event;
                    _prior = new Lazy<Event>(() => v);
                    if (value != null)
                        _idEventBinding = v.Id;
                    NotifyPropertyChanged(nameof(Prior));
                }
            }
        }

        private Lazy<Event> _next;
        public IEvent Next
        {
            get { return _next.Value; }
            protected set
            {
                if (value != _next.Value)
                {
                    _next = new Lazy<Event>(() => value as Event);
                    NotifyPropertyChanged(nameof(Next));
                    if (value != null)
                        IsLoop = false;
                }
            }
        }

        public void InsertAfter(IEvent e)
        {
            Event eventToInsert = e as Event;
            if (eventToInsert != null)
                lock ((Engine as Engine).RundownSync)
                {
                    Event oldParent = eventToInsert.Parent as Event;
                    Event oldPrior = eventToInsert.Prior as Event;
                    if (oldParent != null)
                        oldParent._subEventsRemove(eventToInsert);
                    if (oldPrior != null)
                        oldPrior.Next = null;

                    Event next = this.Next as Event;
                    if (next == eventToInsert)
                        return;
                    this.Next = eventToInsert;
                    eventToInsert.StartType = TStartType.After;
                    eventToInsert.Prior = this;

                    // notify about relocation
                    eventToInsert.NotifyRelocated();
                    eventToInsert.Next = next;

                    if (next != null)
                        next.Prior = eventToInsert;

                    //time calculations
                    eventToInsert._uppdateScheduledTime();
                    eventToInsert._durationChanged();

                    // save key events
                    eventToInsert.Save();
                    if (next != null)
                        next.Save();
                }
        }

        public void InsertBefore(IEvent e)
        {
            Event eventToInsert = e as Event;
            if (eventToInsert != null)
                lock ((Engine as Engine).RundownSync)
                {
                    Event prior = this.Prior as Event;
                    Event parent = this.Parent as Event;
                    Event oldParent = eventToInsert.Parent as Event;
                    Event oldPrior = eventToInsert.Prior as Event;
                    if (oldParent != null)
                        oldParent._subEventsRemove(eventToInsert);
                    if (oldPrior != null)
                        oldPrior.Next = null;

                    eventToInsert.StartType = _startType;
                    if (prior == null)
                        eventToInsert.IsHold = false;

                    if (parent != null)
                    {
                        parent._subEvents.Value.Remove(this);
                        parent._subEvents.Value.Add(eventToInsert);
                        parent.NotifySubEventChanged(eventToInsert, TCollectionOperation.Insert);
                        Parent = null;
                    }
                    eventToInsert.Parent = parent;
                    eventToInsert.Prior = prior;

                    if (prior != null)
                        prior.Next = eventToInsert;

                    // notify about relocation
                    eventToInsert.NotifyRelocated();
                    this.Prior = eventToInsert;
                    eventToInsert.Next = this;
                    this.StartType = TStartType.After;

                    // time calculations
                    eventToInsert._uppdateScheduledTime();
                    eventToInsert._durationChanged();

                    eventToInsert.Save();
                    this.Save();
                }
        }

        public void InsertUnder(IEvent se, bool fromEnd)
        {
            Event subEventToAdd = se as Event;
            if (subEventToAdd != null)
                lock ((Engine as Engine).RundownSync)
                {
                    Event oldPrior = subEventToAdd.Prior as Event;
                    Event oldParent = subEventToAdd.Parent as Event;
                    if (oldParent != null)
                        oldParent._subEventsRemove(subEventToAdd);
                    if (oldPrior != null)
                        oldPrior.Next = null;
                    if (EventType == TEventType.Container)
                    {
                        if (!(subEventToAdd.StartType == TStartType.Manual || subEventToAdd.StartType == TStartType.OnFixedTime)) // do not change if valid
                            subEventToAdd.StartType = TStartType.Manual;
                    }
                    else
                        subEventToAdd.StartType = fromEnd ? TStartType.WithParentFromEnd : TStartType.WithParent;
                    subEventToAdd.Parent = this;
                    subEventToAdd.IsHold = false;
                    _subEvents.Value.Add(subEventToAdd);
                    NotifySubEventChanged(subEventToAdd, TCollectionOperation.Insert);
                    Duration = _computedDuration();
                    Event prior = subEventToAdd.Prior as Event;
                    if (prior != null)
                    {
                        prior.Next = null;
                        subEventToAdd.Prior = null;
                        prior._durationChanged();
                    }
                    subEventToAdd._uppdateScheduledTime();
                    // notify about relocation
                    subEventToAdd.NotifyRelocated();
                    Event lastToInsert = subEventToAdd.Next as Event;
                    while (lastToInsert != null)
                    {
                        lastToInsert.NotifyRelocated();
                        lastToInsert = lastToInsert.Next as Event;
                    }
                    subEventToAdd.Save();
                }
        }

        private void _subEventsRemove(Event subEventToRemove)
        {
            if (_subEvents.Value.Remove(subEventToRemove))
            {
                Duration = _computedDuration();
                NotifySubEventChanged(subEventToRemove, TCollectionOperation.Remove);
            }
        }

        public void Remove()
        {
            lock ((Engine as Engine).RundownSync)
            {
                Event parent = Parent as Event;
                Event next = Next as Event;
                Event prior = Prior as Event;
                TStartType startType = _startType;
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
                    parent._subEventsRemove(this);
                    if (next != null)
                        parent._subEvents.Value.Add(next);
                    if (parent.SetField(ref parent._duration, parent._computedDuration(), "Duration"))
                        parent._durationChanged();
                    if (next != null)
                        parent.NotifySubEventChanged(next, TCollectionOperation.Insert);
                }
                if (prior != null)
                {
                    prior.Next = next;
                    prior._durationChanged();
                }
                if (next != null)
                    next.Save();
                Next = null;
                Prior = null;
                Parent = null;
                _idEventBinding = 0;
                StartType = TStartType.None;
            }
        }

        public void MoveUp()
        {
            lock ((Engine as Engine).RundownSync)
            {
                // this = e3
                Event e2 = Prior as Event;
                Event e4 = Next as Event; // load if nescessary
                Debug.Assert(e2 != null, "Cannot move up - it's the first event");
                if (e2 == null)
                    return;
                Event e2parent = e2.Parent as Event;
                Event e2prior = e2.Prior as Event;
                if (e2parent != null)
                {
                    e2parent._subEvents.Value.Remove(e2);
                    e2parent.NotifySubEventChanged(e2, TCollectionOperation.Remove);
                    e2parent._subEvents.Value.Add(this);
                    e2parent.NotifySubEventChanged(this, TCollectionOperation.Insert);
                }
                if (e2prior != null)
                    e2prior.Next = this;
                StartType = e2._startType;
                AutoStartFlags = e2.AutoStartFlags;
                Prior = e2prior;
                Parent = e2parent;
                _idEventBinding = e2._idEventBinding;
                e2.Prior = this;
                e2.StartType = TStartType.After;
                e2.Next = e4;
                e2.Parent = null;
                Next = e2;
                if (e4 != null)
                    e4.Prior = e2;
                _uppdateScheduledTime();
                if (e4 != null)
                    e4.Save();
                e2.Save();
                Save();
                NotifyRelocated();
            }
        }

        public void MoveDown()
        {
            lock ((Engine as Engine).RundownSync)
            {
                // this = e2
                Event e3 = Next as Event; // load if nescessary
                Debug.Assert(e3 != null, "Cannot move down - it's the last event");
                if (e3 == null)
                    return;
                Event e4 = e3.Next as Event;
                Event e2parent = Parent as Event;
                Event e2prior = Prior as Event;
                if (e2parent != null)
                {
                    e2parent._subEvents.Value.Remove(this);
                    e2parent.NotifySubEventChanged(this, TCollectionOperation.Remove);
                    e2parent._subEvents.Value.Add(e3);
                    e2parent.NotifySubEventChanged(e3, TCollectionOperation.Insert);
                }
                if (e2prior != null)
                    e2prior.Next = e3;
                e3.StartType = _startType;
                e3.AutoStartFlags = _autoStartFlags;
                e3.Prior = e2prior;
                e3.Parent = e2parent;
                e3._idEventBinding = _idEventBinding;
                StartType = TStartType.After;
                e3.Next = this;
                Parent = null;
                Prior = e3;
                Next = e4;
                if (e4 != null)
                    e4.Prior = this;
                e3._uppdateScheduledTime();
                if (e4 != null)
                    e4.Save();
                Save();
                e3.Save();
                NotifyRelocated();
            }
        }

        /// <summary>
        /// Gets time of event that requires attention event, or null if event does not contain such an element
        /// </summary>
        /// <returns></returns> 
        public Nullable<TimeSpan> GetAttentionTime()
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

        public TimeSpan? Offset
        {
            get
            {
                var rrt = _requestedStartTime;
                if (rrt != null)
                    return _scheduledTime.TimeOfDay - rrt;
                return null;
            }
        }
        
        public void Save()
        {
            try
            {
                if (_id == 0)
                    this.DbInsert();
                else
                    this.DbUpdate();
                IsModified = false;
                NotifySaved();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception saving event {0}", EventName);
            }
        }

        internal Event FindVisibleSubEvent()
        {
            if (_eventType != TEventType.Rundown)
                throw new InvalidOperationException("FindVisibleSubEvent: EventType is not Rundown");
            var se = SubEvents.FirstOrDefault(e => ((e.EventType == TEventType.Live || e.EventType == TEventType.Movie) && e.Layer == VideoLayer.Program) || e.EventType == TEventType.Rundown) as Event;
            if (se != null && se.EventType == TEventType.Rundown)
                return se.FindVisibleSubEvent();
            else
                return se;
        }

        public void SaveLoadedTree()
        {
            if (IsModified && _engine != null)
                Save();
            var se = _subEvents;
            if (se != null && se.IsValueCreated && se.Value != null)
            {
                foreach (Event e in se.Value)
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

        public bool AllowDelete()
        {
            if ((_playState == TPlayState.Fading || _playState == TPlayState.Paused || _playState == TPlayState.Playing) &&
                (_eventType == TEventType.Live || _eventType == TEventType.Movie || _eventType == TEventType.Rundown))
                return false;
            if (_eventType == TEventType.Container && SubEvents.Any())
                return false;
            foreach (IEvent se in this.SubEvents)
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
    

        private bool _isDeleted = false;
        public bool IsDeleted { get { return _isDeleted; } }
        public void Delete()
        {
            if (!IsDeleted && AllowDelete())
                _delete();
        }

        protected void _delete()
        {
            Remove();
            foreach (IEvent se in SubEvents)
            {
                Event ne = se as Event;
                while (ne != null)
                {
                    var next = ne.Next as Event;
                    ne._delete();
                    ne = next;
                }
                ((Event)se)._delete();
            }
            _isDeleted = true;
            this.DbDelete();
            NotifyDeleted();
            _isModified = false;
            Dispose();
        }

        public MediaDeleteDenyReason CheckCanDeleteMedia(IServerMedia media)
        {
            Event nev = this;
            while (nev != null)
            {
                if (nev.EventType == TEventType.Movie
                    && nev.Media == media
                    && nev.ScheduledTime >= Engine.CurrentTime)
                    return new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.MediaInFutureSchedule, Event = nev, Media = media };
                foreach (Event se in nev._subEvents.Value.ToList())
                {
                    MediaDeleteDenyReason reason = se.CheckCanDeleteMedia(media);
                    if (reason.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                        return reason;
                }
                nev = nev.Next as Event;
            }
            return MediaDeleteDenyReason.NoDeny;
        }

        private bool _isCGEnabled;
        [JsonProperty]
        public bool IsCGEnabled { get { return _isCGEnabled; } set { SetField(ref _isCGEnabled, value, nameof(IsCGEnabled)); } }
        private byte _crawl;
        [JsonProperty]
        public byte Crawl { get { return _crawl; } set { SetField(ref _crawl, value, nameof(Crawl)); } }
        private byte _logo;
        [JsonProperty]
        public byte Logo { get { return _logo; }  set { SetField(ref _logo, value, nameof(Logo)); } }
        private byte _parental;
        [JsonProperty]
        public byte Parental { get { return _parental; } set { SetField(ref _parental, value, nameof(Parental)); } }
        
        public override string ToString()
        {
            return EventName;
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

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (base.SetField(ref field, value, propertyName))
            {
                IsModified = true;
                return true;
            }
            return false;
        }

        public event EventHandler Relocated;
        protected virtual void NotifyRelocated()
        {
            Relocated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Deleted;
        protected virtual void NotifyDeleted()
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Saved;
        protected virtual void NotifySaved()
        {
            Saved?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;
        protected virtual void NotifySubEventChanged(Event e, TCollectionOperation operation)
        {
            SubEventChanged?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, operation));
        }
    }

}
