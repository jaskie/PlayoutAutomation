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

namespace TAS.Server
{

    public class Event : DtoBase, IEvent, IComparable
    {

        public Event(
                    IEngine engine,
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
                    EventGPI gpi)
        {
            Engine = engine;
            _idRundownEvent = idRundownEvent;
            _idEventBinding = idEventBinding;
            _layer = videoLayer;
            _eventType = eventType;
            _startType = startType;
            _playState = playState;
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

             _subEvents = new Lazy<SynchronizedCollection<IEvent>>(() =>
             {
                 var result = new SynchronizedCollection<IEvent>();
                 if (_idRundownEvent != 0)
                 {
                     Engine.DbReadSubEvents(this, result);
                     foreach (Event e in result)
                     {
                         e._startType = TStartType.With;
                         e.Parent = this;
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
        
        UInt64 _idRundownEvent = 0;
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

        public IEvent Clone()
        {
            Event newEvent = new Event(
                Engine,
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
                IEvent newSubevent = e.Clone();
                newEvent.InsertUnder(newSubevent);
                IEvent ne = e.Next;
                while (ne != null)
                {
                    IEvent nec = ne.Clone();
                    newSubevent.InsertAfter(nec);
                    newSubevent = nec;
                    ne = ne.Next;
                }
            }
            return newEvent;
        }

         TPlayState _playState = TPlayState.Scheduled;
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
        public SynchronizedCollection<IEvent> SubEvents { get { return _subEvents.Value; } }

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

        public IEngine Engine { get; private set; }

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
            set { SetField(ref _startType, value, "StartType"); }
        }

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
                    nt = Engine.AlignDateTime(pev.ScheduledTime + _scheduledDelay);
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
                return _isEnabled ? _duration + _scheduledDelay : TimeSpan.Zero;
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
            get
            {
                return _requestedStartTime;
            }
            set
            {
                if (SetField(ref _requestedStartTime, value, "RequestedStartTime"))
                {
                    NotifyPropertyChanged("Offset");
                }
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
                Event parent = _parent.Value;
                if (_eventType == TEventType.StillImage && parent != null && _scheduledDelay == TimeSpan.Zero)
                    return parent.TransitionTime;
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
            get
            {
                Event parent = _parent.Value;
                if (_eventType == TEventType.StillImage && parent != null && _scheduledDelay == TimeSpan.Zero)
                    return parent._transitionType;
                return _transitionType;
            }
            set { SetField(ref _transitionType, value, "TransitionType"); }
        }

        Guid _mediaGuid;
        public Guid MediaGuid
        {
            get { return _mediaGuid; }
            set
            {
                if (SetField(ref _mediaGuid, value, "MediaGuid"))
                {
                    NotifyPropertyChanged("Media");
                    _serverMediaPRI = null;
                    _serverMediaSEC = null;
                    _serverMediaPRV = null;
                }
            }
        }

        public IMedia Media
        {
            get
            {
                return ServerMediaPRI;
            }
            set
            {
                var newMedia = value as ServerMedia;
                var oldMedia = _serverMediaPRI;
                if (SetField(ref _serverMediaPRI, newMedia, "Media"))
                {
                    _mediaGuid = newMedia == null ? Guid.Empty : newMedia.MediaGuid;
                    _serverMediaPRV = null;
                    _serverMediaSEC = null;
                    if (newMedia != null)
                        newMedia.PropertyChanged += _serverMediaPRI_PropertyChanged;
                    if (oldMedia != null)
                        oldMedia.PropertyChanged -= _serverMediaPRI_PropertyChanged;
                }

            }
        }

        private void _serverMediaPRI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AudioVolume" && this.AudioVolume == null)
                NotifyPropertyChanged("AudioVolume");
        }

        private ServerMedia _serverMediaPRI;
        public IServerMedia ServerMediaPRI
        {
            get
            {
                var media = _serverMediaPRI;
                if (media != null)
                    return media;
                Guid mediaGuid = _mediaGuid;
                if (media == null && mediaGuid != Guid.Empty)
                {
                    MediaDirectory dir = (MediaDirectory)Engine.MediaManager.MediaDirectoryPRI;
                    if (dir != null)
                    {
                        var newMedia = dir.FindMediaByMediaGuid(mediaGuid);
                        if (newMedia is ServerMedia)
                        {
                            _serverMediaPRI = (ServerMedia)newMedia;
                            newMedia.PropertyChanged += _serverMediaPRI_PropertyChanged;
                        }
                    }
                }
                return _serverMediaPRI;
            }
        }

        private ServerMedia _serverMediaSEC;
        public IServerMedia ServerMediaSEC
        {
            get
            {
                var media = _serverMediaSEC;
                if (media != null)
                    return media;
                Guid mediaGuid = _mediaGuid;
                if (media == null && mediaGuid != Guid.Empty)
                {
                    MediaDirectory dir = (MediaDirectory)Engine.MediaManager.MediaDirectorySEC;
                    if (dir != null)
                    {
                        var newMedia = dir.FindMediaByMediaGuid(mediaGuid);
                        if (newMedia is ServerMedia)
                            _serverMediaSEC = (ServerMedia)newMedia;
                    }
                }
                return _serverMediaSEC;
            }
        }


        private ServerMedia _serverMediaPRV;
        public IServerMedia ServerMediaPRV
        {
            get
            {
                Guid mediaGuid = _mediaGuid;
                var media = _serverMediaPRV;
                if ((media == null || media.MediaStatus == TMediaStatus.Deleted) && mediaGuid != Guid.Empty)
                {
                    MediaDirectory dir = (MediaDirectory)Engine.MediaManager.MediaDirectoryPRV;
                    if (dir != null)
                    {
                        var newMedia = dir.FindMediaByMediaGuid(mediaGuid);
                        if (newMedia is ServerMedia)
                            _serverMediaPRV = (ServerMedia)newMedia;
                    }
                }
                return _serverMediaPRV;
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
                    {
                        StartType = TStartType.With;
                        _idEventBinding = value.IdRundownEvent;
                    }
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
                    {
                        StartType = TStartType.After;
                        _idEventBinding = value.IdRundownEvent;
                    }
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
                        parent.SubEvents.Remove(this);
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
                        subEventToAdd.StartType = TStartType.Manual;
                    else
                        subEventToAdd.StartType = TStartType.With;
                    subEventToAdd.Parent = this;
                    subEventToAdd.IsHold = false;
                    SubEvents.Add(subEventToAdd);
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
            if (SubEvents.Remove(subEventToRemove))
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
                Prior = e2prior;
                Parent = e2parent;
                Next = e2;
                e2.Prior = this;
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
                e3.Prior = e2prior;
                e3.Parent = e2parent;
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
            var se = _subEvents.Value;
            if (se != null)
            {
                foreach (Event e in se)
                {
                    Event ce = e;
                    do
                    {
                        ce.SaveLoadedTree();
                        Event ne = ce._next.Value;
                        ce = ne;
                    } while (ce != null);
                }
            }
        }

        public bool AllowDelete()
        {
                if (_playState == TPlayState.Fading || _playState == TPlayState.Paused || _playState == TPlayState.Playing)
                    return false;
                foreach (IEvent se in this.SubEvents.ToList())
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
            {
                Remove();
                foreach (IEvent se in SubEvents.ToList())
                {
                    IEvent ne = se;
                    while (ne != null)
                    {
                        var next = ne.Next;
                        ne.Delete();
                        ne = next;
                    }
                    se.Delete();
                }
                _isDeleted = true;
                this.DbDelete();
                NotifyDeleted();
                _modified = false;
            }
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
                if (nev.SubEvents != null)
                {
                    foreach (Event se in nev._subEvents.Value.ToList())
                    {
                        MediaDeleteDenyReason reason = se.CheckCanDeleteMedia(media);
                        if (reason.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                            return reason;
                    }
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
            var handler = Relocated;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler Deleted;
        protected virtual void NotifyDeleted()
        {
            var handler = Deleted;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler Saved;
        protected virtual void NotifySaved()
        {
            var handler = Saved;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> SubEventChanged;
        protected virtual void NotifySubEventChanged(Event e, TCollectionOperation operation)
        {
            var handler = SubEventChanged;
            if (handler != null)
                handler(this, new CollectionOperationEventArgs<IEvent>(e, operation));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
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
