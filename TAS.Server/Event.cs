
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

namespace TAS.Server
{
    public class Event : DtoBase, IEvent, IComparable
    {

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
                    TTransitionType transitionType,
                    decimal? audioVolume,
                    UInt64 idProgramme,
                    string idAux,
                    bool isEnabled,
                    bool isHold,
                    bool isLoop,
                    EventGPI gpi,
                    AutoStartFlags autoStartFlags)
        {
            _engine = engine;
            _idRundownEvent = idRundownEvent;
            _idEventBinding = idEventBinding;
            _layer = videoLayer;
            _eventType = eventType;
            _startType = startType;
            _playState = playState == TPlayState.Paused ? TPlayState.Scheduled: playState;
            _scheduledTime = scheduledTime;
            _duration = duration;
            _scheduledDelay = scheduledDelay;
            _scheduledTc = scheduledTC;
            _mediaGuid = mediaGuid;
            _eventName = eventName;
            _startTime = startTime;
            _startTc = startTC;
            _requestedStartTime = requestedStartTime;
            _transitionTime = transitionTime;
            _transitionType = transitionType;
            _audioVolume = audioVolume;
            _idProgramme = idProgramme;
            _idAux = idAux;
            _isEnabled = isEnabled;
            _isHold = isHold;
            _isLoop = isLoop;
            _gPI = gpi;
            _autoStartFlags = autoStartFlags;
            _applyMedia(null);
             _subEvents = new Lazy<SynchronizedCollection<IEvent>>(() =>
             {
                 var result = new SynchronizedCollection<IEvent>();
                 if (_idRundownEvent != 0)
                 {
                     Engine.DbReadSubEvents(this, result);
                     foreach (Event e in result)
                         e.Parent = this;
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
                if (_startType == TStartType.After && _idEventBinding > 0)
                    prior = (Event)Engine.DbReadEvent(_idEventBinding);
                return prior;
            });

            _parent = new Lazy<Event>(() =>
            {
                if (_startType == TStartType.With && _idEventBinding > 0)
                    return (Event)Engine.DbReadEvent(_idEventBinding);
                return null;
            });
        }

#if DEBUG
        ~Event()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif

        UInt64 _idRundownEvent = 0;
        [XmlIgnore]
        public UInt64 IdRundownEvent
        {
            get
            {
                if (_idRundownEvent == 0)
                    Save();
                return _idRundownEvent;
            }
            set { _idRundownEvent = value; }
        }
        UInt64 _idEventBinding;

        private bool _modified;
        public bool Modified
        {
            get { return _modified; }
            set
            {
                if (_modified != value)
                {
                    _modified = value;
                    NotifyPropertyChanged("Modified");
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
            return (timecomp == 0) ? this.IdRundownEvent.CompareTo((obj as Event).IdRundownEvent) : timecomp;
        }

        public object Clone()
        {
            IEvent newEvent = Engine.AddNewEvent(
                0,
                0,
                _layer,
                _eventType,
                _startType,
                TPlayState.Scheduled,
                _scheduledTime,
                _duration,
                _scheduledDelay,
                _scheduledTc,
                _mediaGuid,
                _eventName,
                _startTime,
                _startTc,
                _requestedStartTime,
                _transitionTime,
                _transitionType,
                _audioVolume,
                _idProgramme,
                _idAux,
                _isEnabled,
                _isHold,
                _isLoop,
                _gPI);

            foreach (Event e in SubEvents)
            {
                IEvent newSubevent = (IEvent)e.Clone();
                newEvent.InsertUnder(newSubevent);
                IEvent ne = e.Next;
                while (ne != null)
                {
                    IEvent nec = (IEvent)ne.Clone();
                    newSubevent.InsertAfter(nec);
                    newSubevent = nec;
                    ne = ne.Next;
                }
            }
            return newEvent;
        }

        TPlayState _playState;
        public TPlayState PlayState
        {
            get { return _playState; }
            set
            {
                if (SetField(ref _playState, value, "PlayState"))
                {
                    if (value == TPlayState.Playing)
                    {
                        StartTime = Engine.CurrentTime;
                        StartTc = ScheduledTc + TimeSpan.FromTicks(_position * Engine.FrameTicks);
                    }
                    if (value == TPlayState.Scheduled)
                    {
                        StartTime = default(DateTime);
                        StartTc = ScheduledTc;
                        Position = 0;
                        UpdateScheduledTime(false);
                    }
                }
            }
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
                    var h = PositionChanged;
                    if (h != null)
                        h(this, new EventPositionEventArgs(value, _duration - TimeSpan.FromTicks(Engine.FrameTicks * value)));
                }
            }
        }

        public event EventHandler<EventPositionEventArgs> PositionChanged;

        public bool IsFinished { get { return _position >= LengthInFrames; } }

        public UInt64 IdEventBinding { get { return _idEventBinding; } }

        Lazy<SynchronizedCollection<IEvent>> _subEvents;
        public IList<IEvent> SubEvents { get { lock (_subEvents.Value.SyncRoot)  return _subEvents.Value.ToList(); } }

        public int SubEventsCount { get { return _subEvents.Value.Count; } }

        public bool IsContainedIn(IEvent parent)
        {
            IEvent pe = this;
            while (true)
            {
                if (pe == null)
                    return false;
                if (pe == parent)
                    return true;
                pe = pe.VisualParent;
            }
        }

        public IEnumerable<IEvent> GetVisualRootTrack()
        {
            IEvent pe = this;
            while (pe != null)
            {
                yield return pe;
                pe = pe.VisualParent;
            }
        }

        public IEvent VisualParent
        {
            get
            {
                IEvent ev = this;
                IEvent pev = ev.Prior;
                while (pev != null)
                {
                    ev = ev.Prior;
                    pev = ev.Prior;
                }
                return ev.Parent;
            }
        }

        private readonly Engine _engine;
        public IEngine Engine { get { return _engine; } }

         VideoLayer _layer = VideoLayer.None;
        public VideoLayer Layer
        {
            get { return _layer; }
            set { SetField(ref _layer, value, "Layer"); }
        }

         TEventType _eventType;
        public TEventType EventType
        {
            get { return _eventType; }
            set
            {
                if (SetField(ref _eventType, value, "EventType"))
                    if (value == TEventType.Live || value == TEventType.Rundown)
                    {
                        _serverMediaPRI = null;
                        _serverMediaSEC = null;
                        _serverMediaPRV = null;
                    }
            }
        }

        TStartType _startType;
        public TStartType StartType
        {
            get { return _startType; }
            set
            {
                var oldValue = _startType;
                if (SetField(ref _startType, value, "StartType"))
                {
                    if (value == TStartType.OnFixedTime)
                        _engine.AddFixedTimeEvent(this);
                    if (oldValue == TStartType.OnFixedTime)
                        _engine.RemoveFixedTimeEvent(this);
                }
            }
        }

        AutoStartFlags _autoStartFlags;
        public AutoStartFlags AutoStartFlags { get { return _autoStartFlags; } set { SetField(ref _autoStartFlags, value, "AutoStartFlags"); } }

        public DateTime EndTime
        {
            get
            {
                if (_playState == TPlayState.Scheduled)
                {
                    DateTime et = ScheduledTime;
                    if (_isEnabled)
                    {
                        if (_eventType == TEventType.Rundown)
                        {
                            foreach (IEvent se in SubEvents)
                            {
                                IEvent le = se;
                                IEvent le_n = le.Next;
                                while (le_n != null)
                                {
                                    le = le_n;
                                    le_n = le.Next;
                                }
                                DateTime le_t = le.EndTime;
                                if (le_t > et)
                                    et = le_t;
                            }
                        }
                        else
                            et = ScheduledTime + Length;
                    }
                    return et;
                }
                if (_playState == TPlayState.Played || _playState == TPlayState.Aborted)
                {
                    long val = StartTime.Ticks + Length.Ticks + (StartTc.Ticks - ScheduledTc.Ticks);
                    if (val > 0)
                        return new DateTime(val);
                    else
                        return default(DateTime);
                }
                // playstate playing, fading
                return Engine.CurrentTime + _duration - TimeSpan.FromTicks(Engine.FrameTicks * _position);
            }
        }

        public void UpdateScheduledTime(bool updateSuccessors)
        {
            DateTime nt = _scheduledTime;
            IEvent pev = null;
            if (StartType == TStartType.After)
                pev = Prior;
            if (pev != null)
                nt = Engine.AlignDateTime(pev.EndTime - TransitionTime);
            else
            {
                pev = VisualParent;
                if (pev != null && pev.EventType != TEventType.Container)
                    nt = Engine.AlignDateTime(pev.ScheduledTime);
            }
            if (SetField(ref _scheduledTime, nt, "ScheduledTime"))
            {
                foreach (Event ev in SubEvents) //update all sub-events
                    ev.UpdateScheduledTime(true);
                if (updateSuccessors)
                {
                    IEvent ne = Next;
                    if (ne == null)
                    {
                        IEvent vp = VisualParent;
                        if (vp != null)
                            ne = vp.Next;
                    }
                    if (ne != null)
                        ne.UpdateScheduledTime(true);
                }
                NotifyPropertyChanged("Offset");
            }
        }


        DateTime _scheduledTime;
        public DateTime ScheduledTime
        {
            get
            {
                if (_playState == TPlayState.Scheduled
                    || _startTime == default(DateTime))
                    return _scheduledTime;
                else
                    return _startTime;
            }
            set
            {
                value = Engine.AlignDateTime(value);
                if (SetField(ref _scheduledTime, value, "ScheduledTime"))
                {
                    Event ne = _next.Value;
                    if (ne != null)
                        ne.UpdateScheduledTime(true);  // trigger update all next events
                    foreach (Event ev in SubEvents) //update all sub-events
                        ev.UpdateScheduledTime(true);
                    NotifyPropertyChanged("Offset");
                }
            }
        }


        public TimeSpan Length
        {
            get
            {
                return _isEnabled ? _duration : TimeSpan.Zero;
            }
        }

        bool _isEnabled = true;
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (SetField(ref _isEnabled, value, "IsEnabled"))
                {
                    DurationChanged();
                    Event ne = _next.Value;
                    if (ne != null)
                        ne.UpdateScheduledTime(true);
                }
            }
        }

        bool _isHold;
        public bool IsHold
        {
            get
            {
                //Event parent = Parent;
                //return (parent == null) ? _hold : parent.Hold;
                return _isHold;
            }
            set { SetField(ref _isHold, value, "IsHold"); }
        }

        bool _isLoop;
        public bool IsLoop { get { return _isLoop; } set { SetField(ref _isLoop, value, "IsLoop"); } }

        bool _isForcedNext;
        public bool IsForcedNext { get { return _isForcedNext; } set { SetField(ref _isForcedNext, value, "IsForcedNext"); } }

        DateTime _startTime;
        public DateTime StartTime
        {
            get
            {
                return _startTime;
            }
            internal set
            {
                if (SetField(ref _startTime, value, "StartTime"))
                {
                    if (value != default(DateTime))
                    {
                        ScheduledTime = value;
                        IEvent succ = GetSuccessor();
                        if (succ != null)
                            succ.UpdateScheduledTime(true);
                    }
                }
            }
        }

        TimeSpan _scheduledDelay;
        public TimeSpan ScheduledDelay
        {
            get { return _scheduledDelay; }
            set { SetField(ref _scheduledDelay, Engine.AlignTimeSpan(value), "ScheduledDelay"); }
        }

        TimeSpan _duration;
        public TimeSpan Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                TimeSpan newDuration = Engine.AlignTimeSpan(value);
                if (newDuration != _duration)
                {
                    if (_eventType == TEventType.Live || _eventType == TEventType.Movie)
                    {
                        foreach (Event e in SubEvents.Where(ev => ev.EventType == TEventType.StillImage))
                        {
                            TimeSpan nd = e._duration + newDuration - this._duration;
                            e.Duration = nd > TimeSpan.Zero ? nd : TimeSpan.Zero;
                        }
                    }
                    if (SetField(ref _duration, newDuration, "Duration"))
                        DurationChanged();
                }
            }
        }

        private TimeSpan ComputedDuration()
        {
            if (_eventType == TEventType.Rundown)
            {
                long maxlen = 0;
                foreach (IEvent e in SubEvents)
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
                return Engine.AlignTimeSpan(TimeSpan.FromTicks(maxlen));
            }
            else
                return _duration;
        }

        private void DurationChanged()
        {
            if (_eventType == TEventType.Movie || _eventType == TEventType.Rundown || _eventType == TEventType.Live)
            {
                Event ev = Next as Event;
                if (ev != null)
                    ev.UpdateScheduledTime(true);
                ev = VisualParent as Event;
                if (ev != null && ev._eventType == TEventType.Rundown)
                {
                    var t = ev.ComputedDuration();
                    if (!ev.Duration.Equals(t))
                    {
                        ev.SetField(ref ev._duration, t, "Duration");
                        ev.DurationChanged();
                    }
                }
            }
        }

        TimeSpan _scheduledTc = TimeSpan.Zero;
        public TimeSpan ScheduledTc
        {
            get
            {
                return _scheduledTc;
            }
            set
            {
                value = Engine.AlignTimeSpan(value);
                SetField(ref _scheduledTc, value, "ScheduledTc");
            }
        }

        TimeSpan _startTc = TimeSpan.Zero;
        public TimeSpan StartTc
        {
            get
            {
                return _startTc;
            }
            set
            {
                value = Engine.AlignTimeSpan(value);
                SetField(ref _startTc, value, "StartTc");
            }
        }

        TimeSpan? _requestedStartTime;
        public TimeSpan? RequestedStartTime  // informational only: when it should run according to schedule. Usefull when adding or removing previous events
        {
            get { return _requestedStartTime; }
            set
            {
                if (SetField(ref _requestedStartTime, value, "RequestedStartTime"))
                    NotifyPropertyChanged("Offset");
            }
        }

        public long LengthInFrames
        {
            get { return Length.Ticks / Engine.FrameTicks; }
        }

        TimeSpan _transitionTime;
        public TimeSpan TransitionTime
        {
            get
            {
                if (_isHold)
                    return TimeSpan.Zero;
                return _transitionTime;
            }
            set
            {
                value = Engine.AlignTimeSpan(value);
                if (SetField(ref _transitionTime, value, "TransitionTime"))
                    UpdateScheduledTime(true);
            }
        }

        TTransitionType _transitionType;
        public TTransitionType TransitionType
        {
            get { return _transitionType; }
            set { SetField(ref _transitionType, value, "TransitionType"); }
        }

        Guid _mediaGuid;
        public Guid MediaGuid
        {
            get { return _mediaGuid; }
            set
            {
                if (SetField(ref _mediaGuid, value, "MediaGuid"))
                    _applyMedia(null);
            }
        }

        public IMedia Media
        {
            get { return ServerMediaPRI; }
            set
            {
                var newMedia = value as PersistentMedia;
                if (newMedia != null && SetField(ref _mediaGuid, newMedia.MediaGuid, "MediaGuid"))
                    _applyMedia(newMedia);
            }
        }

        private void _serverMediaPRI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AudioVolume" && this.AudioVolume == null)
                NotifyPropertyChanged("AudioVolume");
        }

        private void _applyMedia(PersistentMedia initialMedia)
        {
            var serverMediaPRI = _serverMediaPRI;
            if (serverMediaPRI != null && serverMediaPRI.IsValueCreated && serverMediaPRI.Value != null)
                serverMediaPRI.Value.PropertyChanged -= _serverMediaPRI_PropertyChanged;
            var mediaGuid = _mediaGuid;
            if (mediaGuid != Guid.Empty)
            {
                _serverMediaPRI = new Lazy<PersistentMedia>(() =>
                    {
                        var media = initialMedia != null ? initialMedia : _getMediaFromDir(mediaGuid, _eventType == TEventType.Animation ? (MediaDirectory)Engine.MediaManager.AnimationDirectoryPRI : (MediaDirectory)Engine.MediaManager.MediaDirectoryPRI);
                        if (media != null)
                            media.PropertyChanged += _serverMediaPRI_PropertyChanged;
                        return media;
                    });
                _serverMediaSEC = new Lazy<PersistentMedia>(() => _getMediaFromDir(mediaGuid,_eventType == TEventType.Animation ? (MediaDirectory)Engine.MediaManager.AnimationDirectorySEC : (MediaDirectory)Engine.MediaManager.MediaDirectorySEC));
                _serverMediaPRV = new Lazy<PersistentMedia>(() => _getMediaFromDir(mediaGuid, _eventType == TEventType.Animation ? (MediaDirectory)Engine.MediaManager.AnimationDirectoryPRV : (MediaDirectory)Engine.MediaManager.MediaDirectoryPRV));
                NotifyPropertyChanged("Media");
            }
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

        string _eventName;
        public string EventName
        {
            get { return _eventName; }
            set { SetField(ref _eventName, value, "EventName"); }
        }

        Lazy<Event> _parent;
        public IEvent Parent
        {
            get { return _parent.Value; }
            protected set            {
                if (value != _parent.Value)
                {
                    _parent = new Lazy<Event>(() => value as Event);
                    if (value != null)
                        _idEventBinding = value.IdRundownEvent;
                    NotifyPropertyChanged("Parent");
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
                    _prior = new Lazy<Event>(() => value as Event);
                    if (value != null)
                        _idEventBinding = value.IdRundownEvent;
                    NotifyPropertyChanged("Prior");
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
                    NotifyPropertyChanged("Next");
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
                    if (oldPrior != null)
                        oldPrior.DurationChanged();
                    eventToInsert.UpdateScheduledTime(false);
                    eventToInsert.DurationChanged();

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
                    eventToInsert.UpdateScheduledTime(false);
                    eventToInsert.DurationChanged();

                    eventToInsert.Save();
                    this.Save();
                }
        }

        public void InsertUnder(IEvent se)
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
                        subEventToAdd.StartType = TStartType.With;
                    subEventToAdd.Parent = this;
                    subEventToAdd.IsHold = false;
                    _subEvents.Value.Add(subEventToAdd);
                    NotifySubEventChanged(subEventToAdd, TCollectionOperation.Insert);
                    Duration = ComputedDuration();
                    Event prior = subEventToAdd.Prior as Event;
                    if (prior != null)
                    {
                        prior.Next = null;
                        subEventToAdd.Prior = null;
                        prior.DurationChanged();
                    }
                    subEventToAdd.UpdateScheduledTime(true);
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
                Duration = ComputedDuration();
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
                        next.UpdateScheduledTime(true);
                }
                if (parent != null)
                {
                    parent._subEventsRemove(this);
                    if (next != null)
                        parent._subEvents.Value.Add(next);
                    if (parent.SetField(ref parent._duration, parent.ComputedDuration(), "Duration"))
                        parent.DurationChanged();
                    if (next != null)
                        parent.NotifySubEventChanged(next, TCollectionOperation.Insert);
                }
                if (prior != null)
                {
                    prior.Next = next;
                    prior.DurationChanged();
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
                Next = e2;
                _idEventBinding = e2._idEventBinding;
                e2.Prior = this;
                e2.StartType = TStartType.After;
                e2.Next = e4;
                e2.Parent = null;
                if (e4 != null)
                    e4.Prior = e2;
                UpdateScheduledTime(true);
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
                e3.UpdateScheduledTime(true);
                if (e4 != null)
                    e4.Save();
                Save();
                e3.Save();
                NotifyRelocated();
            }
        }
        /// <summary>
        /// Gets subsequent event that will play after this
        /// </summary>
        /// <returns></returns>
        public IEvent GetSuccessor()
        {
            if (_eventType == TEventType.Movie || _eventType == TEventType.Live || _eventType == TEventType.Rundown)
            {
                IEvent nev = Next;
                if (nev != null)
                {
                    IEvent n = nev.Next;
                    while (nev != null && n != null && nev.Length.Equals(TimeSpan.Zero))
                    {
                        nev = nev.Next;
                        n = nev.Next;
                    }
                }
                if (nev == null)
                {
                    nev = VisualParent;
                    if (nev != null)
                        nev = nev.GetSuccessor();
                }
                return nev;
            }
            return null;
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
                IEvent ev = SubEvents.FirstOrDefault(e => e.EventType == TEventType.Movie || e.EventType == TEventType.Live || e.EventType == TEventType.Rundown);
                while (ev != null)
                {
                    TimeSpan? pt = ev.GetAttentionTime();
                    if (pt.HasValue)
                        return pauseTime + pt.Value;
                    pauseTime += ev.Length - ev.TransitionTime;
                    ev = ev.Next;
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
            if (_idRundownEvent == 0)
                this.DbInsert();
            else
                this.DbUpdate();
            _modified = false;
            NotifySaved();
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
            if (Modified && Engine != null)
                Save();
            Media = null;
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
            var media = _serverMediaPRI;
            if (media != null && media.IsValueCreated && media.Value != null)
                media.Value.PropertyChanged -= _serverMediaPRI_PropertyChanged;
            _isDeleted = true;
            this.DbDelete();
            NotifyDeleted();
            _modified = false;
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

        UInt64 _idProgramme;
        public UInt64 IdProgramme
        {
            get
            {
                return _idProgramme;
            }
            set { SetField(ref _idProgramme, value, "IdProgramme"); }
        }    
        
        string _idAux; // auxiliary Id from external system
        public string IdAux
        {
            get
            {
                return _idAux;
            }
            set { SetField(ref _idAux, value, "IdAux"); }
        }

        decimal? _audioVolume;
        public decimal? AudioVolume
        {
            get
            {
                return _audioVolume;
            }
            set { SetField(ref _audioVolume, value, "AudioVolume"); }
        }

        EventGPI _gPI;
        public EventGPI GPI
        {
            get { return _gPI; }
            set { SetField(ref _gPI, value, "GPI"); }
        }

        public decimal GetAudioVolume()
        {
            var volume = _audioVolume;
            if (volume != null)
                return (decimal)volume;
            else
                if (_eventType == TEventType.Movie)
                {
                    var m = Media;
                    if (m != null)
                        return m.AudioVolume;
                }
            return 0m;
        }

        public override string ToString()
        {
            return EventName;
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            lock (this)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return false;
                field = value;
                _modified = true;
            }
            NotifyPropertyChanged(propertyName);
            return true;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class EventCollectionChangedEventArgs : EventArgs
    {
        public EventCollectionChangedEventArgs(Event e, bool removed)
        {
            Event = e;
            Removed = removed;
        }
        public readonly bool Removed;
        public readonly Event Event;
    }


}
