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
using TAS.Data;
using TAS.Server.Interfaces;
using TAS.Server.Common;

namespace TAS.Server
{
 
    public class Event : IEvent, IComparable
    {

        public Event(IEngine AEngine)
        {
            Engine = AEngine;
            AEngine.AddEvent(this);
        }

#if DEBUG
        ~Event()
        {
            Debug.WriteLine(this, "Event Finalized");
        }
#endif // DEBUG

        internal UInt64 _idRundownEvent = 0;
        public UInt64 IdRundownEvent
        {
            get
            {
                if (_idRundownEvent == 0)
                    Save();
                return _idRundownEvent;
            }
            set
            {
                _idRundownEvent = value;
            }
        }
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
            return (timecomp==0) ? this.IdRundownEvent.CompareTo((obj as Event).IdRundownEvent) : timecomp;
         }

        public IEvent Clone()
        {
            Event newEvent = new Event(Engine);
            newEvent.Duration = this.Duration;
            newEvent.Enabled = this.Enabled;
            newEvent.EventName = this.EventName;
            newEvent.EventType = this.EventType;
            newEvent.Hold = this.Hold;
            newEvent.IdAux = this.IdAux;
            newEvent.idProgramme = this.idProgramme;
            newEvent.Layer = this.Layer;
            newEvent.Media = this.Media;
            newEvent.AudioVolume = this.AudioVolume; // must be after media
            newEvent.PlayState = TPlayState.Scheduled;
            newEvent.ScheduledDelay = this.ScheduledDelay;
            newEvent.ScheduledTc = this.ScheduledTc;
            newEvent.ScheduledTime = this.ScheduledTime;
            newEvent.StartType = this.StartType;
            newEvent.TransitionTime = this.TransitionTime;
            newEvent.TransitionType = this.TransitionType;
            newEvent.GPI = this.GPI;
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

        internal bool GPITrigerred;
        internal bool LocalGPITriggered;

        internal TPlayState _playState = TPlayState.Scheduled;
        public TPlayState PlayState
        {
            get { return _playState; }
            set
            {
                if (value != _playState)
                    lock (this)
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
                                GPITrigerred = false;
                                LocalGPITriggered = false;
                                UpdateScheduledTime(false);
                            }
                        }
                    }
            }
        }

        private long _position = 0;
        public long Position // in frames
        {
            get { return _position; }
            set { if (_position != value)
                {
                    _position = value;
                    var h = PositionChanged;
                    if (h != null)
                        h(this, new EventPositionEventArgs(value, _duration - TimeSpan.FromTicks(Engine.FrameTicks * value)));
                }
            }
        }

        public event EventHandler<EventPositionEventArgs> PositionChanged;

        public bool Finished { get { return _position >= LengthInFrames; } }

        internal UInt64 idEventBinding
        {
            get
            {
                if (StartType == TStartType.With)
                    return (Parent == null) ? 0 : Parent.IdRundownEvent;
                else
                    if (StartType == TStartType.After)
                        return (Prior == null) ? 0 : Prior.IdRundownEvent;
                    else
                        if (StartType == TStartType.Manual)
                        {
                            Event parent = Parent as Event;
                            if (parent != null && parent.EventType == TEventType.Container)
                                return parent.IdRundownEvent;
                        }
                return 0;
            }
        }
        internal SynchronizedCollection<IEvent> _subEvents;
        public SynchronizedCollection<IEvent> SubEvents
        {
            get
            {
                lock (this)
                {
                    if (_subEvents == null)
                    {
                        if (_idRundownEvent == 0)
                            _subEvents = new SynchronizedCollection<IEvent>();
                        else
                            _subEvents = this.DbReadSubEvents();
                    }
                    return _subEvents;
                }
            }
        }

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

        public List<IEvent> VisualRootTrack
        {
            get
            {
                List<IEvent> rt = new List<IEvent>();
                IEvent pe = this;
                while (pe != null)
                {
                    rt.Insert(0, pe);
                    pe = pe.VisualParent;
                }
                return rt;
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
        
        public bool IsBefore(IEvent aEvent)
        {
            return (Prior == null) ? false : (Prior == aEvent)? true : Prior.IsBefore(aEvent);
        }

        public IEngine Engine { get; private set; }

        internal VideoLayer _layer = VideoLayer.None;
        public VideoLayer Layer
        {
            get 
            {
                return (_layer == VideoLayer.None) ? ((Parent == null) ? VideoLayer.Program : Parent.Layer) : _layer; 
            }
            set { SetField(ref _layer, value, "Layer"); }
        }
        
        internal TEventType _eventType;
        public TEventType EventType
        {
            get { return _eventType; }
            set
            {
                if (SetField(ref _eventType, value, "EventType"))
                    if (value == TEventType.Live || value == TEventType.Rundown)
                    {
                        _serverMediaPGM = null;
                        _serverMediaPRV = null;
                    }
            }
        }

        internal TStartType _startType;
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
            }
        }


        internal DateTime _scheduledTime;
        public DateTime ScheduledTime
        {
            get
            {
                lock (this)
                {
                    if (_playState == TPlayState.Scheduled || _startTime == default(DateTime))
                        return _scheduledTime;
                    else
                        return _startTime;
                }
            }
            set
            {
                lock (this)
                {
                    value = Engine.AlignDateTime(value);
                    if (SetField(ref _scheduledTime, value, "ScheduledTime"))
                    {
                        if (_next != null)
                            _next.UpdateScheduledTime(true);  // trigger update all next events
                        foreach (Event ev in SubEvents) //update all sub-events
                            ev.UpdateScheduledTime(true);
                    }
                }
            }
        }
        

        public TimeSpan Length
        {
            get 
            {
                return _enabled ? _duration + _scheduledDelay : TimeSpan.Zero; 
            }
        }

        internal bool _enabled = true;
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (SetField(ref _enabled, value, "Enabled"))
                {
                    DurationChanged();
                    if (Next != null)
                        _next.UpdateScheduledTime(true);
                }
            }
        }

        internal bool _hold;
        public bool Hold
        {
            get
            {
                //Event parent = Parent;
                //return (parent == null) ? _hold : parent.Hold;
                return _hold;
            }
            set { SetField(ref _hold, value, "Hold"); }
        }

        internal bool _loop;
        public bool Loop { get { return _loop; } set { SetField(ref _loop, value, "Loop"); } }

        internal DateTime _startTime;
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

        internal TimeSpan _scheduledDelay;
        public TimeSpan ScheduledDelay
        {
            get { return _scheduledDelay; }
            set { SetField(ref _scheduledDelay, Engine.AlignTimeSpan(value), "ScheduledDelay"); }
        }

        internal TimeSpan _duration;
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
                        foreach (Event e in SubEvents.Where(ev => ev.EventType == TEventType.StillImage || ev.EventType == TEventType.AnimationFlash))
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
            lock (this)
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
                                len -= n.Enabled ? n.TransitionTime.Ticks : 0;
                        }
                        if (len > maxlen)
                            maxlen = len;
                    }
                    return Engine.AlignTimeSpan(TimeSpan.FromTicks(maxlen));
                }
                else
                    return _duration;
            }
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

        internal TimeSpan _scheduledTc = TimeSpan.Zero;
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

        internal TimeSpan _startTc = TimeSpan.Zero;
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

        internal TimeSpan? _requestedStartTime;
        public TimeSpan? RequestedStartTime  // informational only: when it should run according to schedule. Usefull when adding or removing previous events
        {
            get
            {
                return _requestedStartTime;
            }
            set { SetField(ref _requestedStartTime, value, "RequestedStartTime"); }
        }

        public long LengthInFrames
        {
            get { return Length.Ticks / Engine.FrameTicks; }
        }

        internal TimeSpan _transitionTime;
        public TimeSpan TransitionTime
        {
            get
            {
                if (_hold)
                    return TimeSpan.Zero;
                Event parent = _parent;
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

        internal TTransitionType _transitionType;
        public TTransitionType TransitionType
        {
            get
            {
                Event parent = _parent;
                if (_eventType == TEventType.StillImage && parent != null && _scheduledDelay == TimeSpan.Zero)
                    return parent._transitionType;
                return _transitionType;
            }
            set { SetField(ref _transitionType, value, "TransitionType"); }
        }

        internal Guid _mediaGuid;
        public Guid MediaGuid
        {
            get
            {
                return _mediaGuid;
            }
        }

        public IMedia Media
        {
            get 
            {
                return ServerMediaPGM;
            }
            set
            {
                var newMedia = value as ServerMedia;
                var oldMedia = _serverMediaPGM;
                if (SetField(ref _serverMediaPGM, newMedia, "Media"))
                {
                    _mediaGuid = newMedia == null ? Guid.Empty : newMedia.MediaGuid;
                    if (newMedia != null)
                        newMedia.PropertyChanged += _serverMediaPGM_PropertyChanged;
                    if (oldMedia != null)
                        oldMedia.PropertyChanged -= _serverMediaPGM_PropertyChanged;
                }

            }
        }

        private void _serverMediaPGM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AudioVolume" && this.AudioVolume == null)
                NotifyPropertyChanged("AudioVolume");
        }
        
        private IServerMedia _serverMediaPRV;
        private IServerMedia _serverMediaPGM;
        public IServerMedia ServerMediaPGM
        {
            get
            {
                var media = _serverMediaPGM;
                if (media != null)
                    return media;
                Guid mediaGuid = _mediaGuid;
                if (media == null && mediaGuid != Guid.Empty)
                {
                    MediaDirectory dir;
                    if (_eventType == TEventType.AnimationFlash)
                        dir = (MediaDirectory)Engine.MediaManager.AnimationDirectoryPGM;
                    else
                        dir = (MediaDirectory)Engine.MediaManager.MediaDirectoryPGM;
                    if (dir != null)
                    {
                        var newMedia = dir.FindMediaByMediaGuid(mediaGuid);
                        if (newMedia is ServerMedia)
                        {
                            _serverMediaPGM = (ServerMedia)newMedia;
                            newMedia.PropertyChanged += _serverMediaPGM_PropertyChanged;
                        }
                    }
                }
                return _serverMediaPGM;
            }
        }
        
        

        public IServerMedia ServerMediaPRV
        {
            get
            {
                Guid mediaGuid = _mediaGuid;
                var media = _serverMediaPRV;
                if ((media == null || media.MediaStatus == TMediaStatus.Deleted) && mediaGuid != Guid.Empty)
                {
                    MediaDirectory dir;
                    if (_eventType == TEventType.AnimationFlash)
                        dir = (MediaDirectory)Engine.MediaManager.AnimationDirectoryPRV;
                    else
                        dir = (MediaDirectory)Engine.MediaManager.MediaDirectoryPRV;
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

        private ITemplate _template;
        internal ITemplate Template
        {
            get
            {
                //Guid mediaGuid = _mediaGuid;
                //if (_template == null)
                //    _template = Engine.MediaManager.Templates.FirstOrDefault(t => t.MediaGuid == this.MediaGuid);
                //return _template;
                throw new NotImplementedException();
            }
        }

        public long SeekPGM
        {
            get
            {
                if (ServerMediaPGM != null)
                {
                    long seek = (this.ScheduledTc.Ticks - ServerMediaPGM.TcStart.Ticks) / Engine.FrameTicks;
                    return (seek < 0) ? 0 : seek;
                }
                return 0;
            }
        }
 
        internal string _eventName;
        public string EventName
        {
            get { return _eventName; }
            set { SetField(ref _eventName, value, "EventName"); }
        }


        internal Event _parent;
        public IEvent Parent
        {
            get
            {
                return _parent;
            }
            protected set { SetField(ref _parent, value as Event, "Parent"); }
        }

        private Event _prior;
        public IEvent Prior 
        {
            get
            {
                return _prior;
            }
            protected set { SetField(ref _prior, value as Event, "Prior"); }
        }

        internal bool _nextLoaded = true;
        private Event _next;
        public IEvent Next
        {
            get
            {
                Event next = _next as Event;
                lock (this)
                {
                    if (!_nextLoaded)
                    {
                        if (next == null)
                        {
                            next = this.DbReadNext();
                            if (next != null)
                                next._prior = this;
                            _next = next;
                            NotifyPropertyChanged("Next");
                        }
                        _nextLoaded = true;
                    }
                }
                return _next;
            }
            protected set {
                if (SetField(ref _next, value as Event, "Next"))
                {
                    _nextLoaded = true;
                    if (value != null)
                        Loop = false;
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
                    {
                        oldPrior.Next = null;
                        oldPrior._nextLoaded = true;
                    }

                    eventToInsert.StartType = _startType;
                    if (prior == null)
                        eventToInsert.Hold = false;

                    if (parent != null)
                    {
                        parent.SubEvents.Remove(this);
                        parent._subEvents.Add(eventToInsert);
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
                    {
                        oldPrior.Next = null;
                        oldPrior._nextLoaded = true;
                    }
                    if (EventType == TEventType.Container)
                        subEventToAdd.StartType = TStartType.Manual;
                    else
                        subEventToAdd.StartType = TStartType.With;
                    subEventToAdd.Parent = this;
                    subEventToAdd.Hold = false;
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
                    next.Save();
                }
                if (parent != null)
                {
                    parent._subEventsRemove(this);
                    if (next != null)
                        parent._subEvents.Add(next);
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
                Next = null;
                Prior = null;
                Parent = null;
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
                Event e1parent = e2.Parent as Event;
                Event e1prior = e2.Prior as Event;
                TStartType e2startType = e2.StartType;
                if (e1parent != null)
                {
                    e1parent._subEvents.Remove(e2);
                    e1parent.NotifySubEventChanged(e2, TCollectionOperation.Remove);
                    e1parent._subEvents.Add(this);
                    e1parent.NotifySubEventChanged(this, TCollectionOperation.Insert);
                }
                if (e1prior != null)
                    e1prior.Next = this;
                StartType = e2startType;
                Prior = e1prior;
                Parent = e1parent;
                Next = e2;
                e2.Prior = this;
                e2.Next = e4;
                e2.StartType = TStartType.After;
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
                Event e1parent = Parent as Event;
                Event e1prior = Prior as Event;
                TStartType e2startType = StartType;
                if (e1parent != null)
                {
                    e1parent._subEvents.Remove(this);
                    e1parent.NotifySubEventChanged(this, TCollectionOperation.Remove);
                    e1parent._subEvents.Add(e3);
                    e1parent.NotifySubEventChanged(e3, TCollectionOperation.Insert);
                }
                if (e1prior != null)
                    e1prior.Next = e3;
                e3.StartType = e2startType;
                e3.Prior = e1prior;
                e3.Parent = e1parent;
                e3.Next = this;
                Prior = e3;
                Next = e4;
                StartType = TStartType.After;
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
            if (_hold || _eventType == TEventType.Live)
                return TimeSpan.Zero;
            if (_eventType == TEventType.Movie)
            {
                IMedia m = Media;
                if (m == null 
                    || m.MediaStatus != TMediaStatus.Available
                    || _scheduledTc<m.TcStart 
                    || _duration+_scheduledTc > m.Duration+m.TcStart)
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


        public void Save()
        {
            if (_modified)
            {
                //_saveMutex.WaitOne();
                if (_idRundownEvent == 0)
                    this.DbInsert();
                else
                    this.DbUpdate();
                _modified = false;
                NotifySaved();
            }
        }

        public void SaveLoadedTree()
        {
            if (Modified && Engine != null)
                Save();
            Media = null;
            var se = _subEvents;
            if (se != null)
            {
                foreach (Event e in se)
                {
                    Event ce = e;
                    do
                    {
                        ce.SaveLoadedTree();
                        Event ne = ce._next;
                        ce = ne;
                    } while (ce != null);
                }
            }
        }
        private bool _isDeleted = false;
        public bool IsDeleted { get { return _isDeleted; } }
        public void Delete()
        {
            lock (this)
            {
                if (!IsDeleted)
                {
                    Remove();
                    foreach (Event se in this.SubEvents.ToList())
                    {
                        IEvent ne = se.Next;
                        while (ne != null)
                        {
                            ne.Delete();
                            var next = ne.Next;
                            ne = next;
                        }
                        se.Delete();
                    }
                    _isDeleted = true;
                    this.DbDelete();
                    Engine.RemoveEvent(this);
                    NotifyDeleted();
                    _modified = false;
                }
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
                if (nev._subEvents != null)
                {
                    foreach (Event se in nev._subEvents.ToList())
                    {
                        MediaDeleteDenyReason reason = se.CheckCanDeleteMedia(media);
                        if (reason.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                            return reason;
                    }
                }
                nev = nev._next;
            }
            return MediaDeleteDenyReason.NoDeny;
        }


        internal UInt64 _idProgramme;
        public UInt64 idProgramme
        {
            get
            {
                return _idProgramme;
            }
            set { SetField(ref _idProgramme, value, "IdProgramme"); }
        }    
        
        internal string _idAux; // auxiliary Id from external system
        public string IdAux
        {
            get
            {
                return _idAux;
            }
            set { SetField(ref _idAux, value, "IdAux"); }
        }

        internal decimal? _audioVolume;
        public decimal? AudioVolume
        {
            get
            {
                return _audioVolume;
            }
            set { SetField(ref _audioVolume, value, "AudioVolume"); }
        }

        internal EventGPI _gPI;
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
