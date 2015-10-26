using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Data;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    
    public class Engine : INotifyPropertyChanged, IDisposable, IEngineConfig
    {
        public UInt64 Id { get; set; }
        public UInt64 Instance { get; set; }
        public UInt64 IdArchive { get; set; }
        public ulong IdServerPGM { get; set; }
        public int ServerChannelPGM { get; set; }
        public ulong IdServerPRV { get; set; }
        public int ServerChannelPRV { get; set; }
        string _engineName;
        public string EngineName
        {
            get { return _engineName; }
            set
            {
                if (value != _engineName)
                {
                    _engineName = value;
                    NotifyPropertyChanged("EngineName");
                }
            }
        }
#region Fields

        [XmlIgnore]
        public readonly MediaManager MediaManager;
        [XmlIgnore]
        public IGpi LocalGpi { get; private set; }

        Thread _engineThread;
        internal long CurrentTicks;
        
        private TimeSpan _preloadTime = new TimeSpan(0, 0, 2); // time to preload event
        internal SimpleDictionary<VideoLayer, Event> _visibleEvents = new SimpleDictionary<VideoLayer, Event>(); // list of visible events
        internal ObservableSynchronizedCollection<Event> _runningEvents = new ObservableSynchronizedCollection<Event>(); // list of events loaded and playing 
        internal SimpleDictionary<VideoLayer, Event> _loadedNextEvents = new SimpleDictionary<VideoLayer, Event>(); // events loaded in backgroud
        private SimpleDictionary<VideoLayer, Event> _finishedEvents = new SimpleDictionary<VideoLayer, Event>(); // events finished or loaded and not playing

        public event EventHandler<EventArgs> EngineTick;
        public event EventHandler<EngineOperationEventArgs> EngineOperation;
        public event EventHandler<PropertyChangedEventArgs> ServerPropertyChanged;
        [XmlElement("Gpi")]
        public GPINotifier GPI;

        public Remoting.RemoteHost Remote { get; set; }
        public TAspectRatioControl AspectRatioControl { get; set; }

        public int TimeCorrection { get { return (int)_timeCorrection.TotalMilliseconds; } set { _timeCorrection = TimeSpan.FromMilliseconds(value); } }
        protected TimeSpan _timeCorrection;
        
#endregion Fields

#region Constructors
        public Engine()
        {
            _visibleEvents.DictionaryOperation += _visibleEventsOperation;
            _loadedNextEvents.DictionaryOperation += _loadedNextEventsOperation;
            _runningEvents.CollectionOperation += _runningEventsOperation;
            EngineState = TEngineState.NotInitialized;
            MediaManager = new MediaManager(this);
        }

#endregion Constructors

#region IDisposable implementation

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                _visibleEvents.DictionaryOperation -= _visibleEventsOperation;
                _loadedNextEvents.DictionaryOperation -= _loadedNextEventsOperation;
                _runningEvents.CollectionOperation -= _runningEventsOperation;
                if (GPI != null)
                    GPI.Dispose();
            }
        }

#endregion //IDisposable

        private PlayoutServerChannel _playoutChannelPGM;

        [XmlIgnore]
        public PlayoutServerChannel PlayoutChannelPGM
        {
            get { return _playoutChannelPGM; }
            set
            {
                var old = _playoutChannelPGM;
                if (old != value)
                {
                    if (old != null)
                    {
                        old.OwnerServer.PropertyChanged -= _onServerPropertyChanged;
                        old.OwnerServer.MediaDirectory.MediaVerified -= _mediaPGMVerified;
                        old.OwnerServer.MediaDirectory.MediaRemoved -= _mediaPGMRemoved;
                        old.Engine = null;
                    }
                    _playoutChannelPGM = value;
                    if (value != null)
                    {
                        value.OwnerServer.PropertyChanged += _onServerPropertyChanged;
                        value.OwnerServer.MediaDirectory.MediaVerified += _mediaPGMVerified;
                        value.OwnerServer.MediaDirectory.MediaRemoved += _mediaPGMRemoved;
                        value.Engine = this;
                    }
                    NotifyPropertyChanged("PlayoutChannelPGM");
                }
            }
        }
        private PlayoutServerChannel _playoutChannelPRV;
        
        [XmlIgnore]
        public PlayoutServerChannel PlayoutChannelPRV
        {
            get { return _playoutChannelPRV; }
            set
            {
                var old = _playoutChannelPRV;
                if (old != value)
                {
                    if (old != null)
                    {
                        old.OwnerServer.PropertyChanged -= _onServerPropertyChanged;
                        old.Engine = null;
                    }
                    _playoutChannelPRV = value;
                    if (value != null)
                    {
                        value.OwnerServer.PropertyChanged += _onServerPropertyChanged;
                        value.Engine = this;
                    }
                    NotifyPropertyChanged("PlayoutChannelPRV");
                }
            }
        }

        long _frameTicks;
        public long FrameTicks { get { return _frameTicks; } }
        public TVideoFormat VideoFormat { get; set; }

        [XmlIgnore]
        public VideoFormatDescription FormatDescription { get; private set; }

        public void Initialize(IGpi localGpi)
        {
            Debug.WriteLine(this, "Begin initializing");
            LocalGpi = localGpi;
            FormatDescription = VideoFormatDescription.Descriptions[VideoFormat];
            _frameTicks = FormatDescription.FrameTicks;
            var chPGM = PlayoutChannelPGM;
            Debug.WriteLine(chPGM, "About to initialize");
            Debug.Assert(chPGM != null && chPGM.OwnerServer != null, "Null channel PGM or its server");
            if (chPGM != null 
                && chPGM.OwnerServer != null)
                ThreadPool.QueueUserWorkItem(o =>
                    chPGM.OwnerServer.Initialize());
            var chPRV = PlayoutChannelPRV;
            if (chPRV != null 
                && chPRV != chPGM
                && chPRV.OwnerServer != null)
                ThreadPool.QueueUserWorkItem(o =>
                    chPRV.OwnerServer.Initialize());

            MediaManager.Initialize();

            Debug.WriteLine(this, "Reading Root Events");
            this.DbReadRootEvents();
            this.DbReadTemplates();

            EngineState = TEngineState.Idle;
            var gpi = GPI;
            if (gpi != null)
            {
                Debug.WriteLine(this, "Initializing GPI");
                gpi.Started += StartLoaded;
                gpi.Initialize();
                gpi.PropertyChanged += GPI_PropertyChanged;
            }

            if (Remote != null)
            {
                Debug.WriteLine(this, "Initializing Remote interface");
                Remote.Initialize(this);
            }

            if (localGpi != null)
                localGpi.Started += StartLoaded;

            Debug.WriteLine(this, "Creating engine thread");
            _engineThread = new Thread(() =>
            {
                Debug.WriteLine(this, "Engine thread started");
                CurrentTime = AlignDateTime(DateTime.UtcNow+_timeCorrection);
                CurrentTicks = CurrentTime.Ticks;
                while (!disposed)
                {
                    try
                    {
                        CurrentTime = AlignDateTime(DateTime.UtcNow+_timeCorrection);
                        long nFrames = (CurrentTime.Ticks - CurrentTicks) / _frameTicks;
                        CurrentTicks = CurrentTime.Ticks;
                        Debug.WriteLineIf(nFrames > 1, this, string.Format("Frame delay - {0}", nFrames));
                        _tick(nFrames);
                        var e = EngineTick;
                        if (e != null)
                            e(this, EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e, "Exception in engine tick");
                    }
                    long timeToWait = (_frameTicks - (DateTime.UtcNow.Ticks + _timeCorrection.Ticks - CurrentTicks)) / TimeSpan.TicksPerMillisecond;
                    if (timeToWait > 0)
                       Thread.Sleep((int)timeToWait);
                }
                Debug.WriteLine(this, "Engine thread finished");
            });
            _engineThread.Priority = ThreadPriority.Highest;
            _engineThread.Name = string.Format("Engine main thread for {0}", EngineName);
            _engineThread.IsBackground = true;
            _engineThread.Start();
            Debug.WriteLine(this, "Engine initialized");
        }

        internal void UnInitialize()
        {
            Debug.WriteLine(this, "Begin uninitializing");

            var ch = PlayoutChannelPGM;
            Debug.WriteLine(this, "Aborting engine thread");
            _engineThread.Abort();
            _engineThread.Join();
            EngineState = TEngineState.NotInitialized;

            if (Remote != null)
            {
                Debug.WriteLine(this, "UnInitializing Remote interface");
                Remote.UnInitialize(this);
            }
            var localGpi = LocalGpi;
            if (localGpi != null)
                localGpi.Started -= StartLoaded;

            var gpi = GPI;
            if (gpi != null)
            {
                Debug.WriteLine(this, "Uninitializing GPI");
                gpi.Started -= StartLoaded;
                gpi.UnInitialize();
                gpi.PropertyChanged -= GPI_PropertyChanged;
            }

            Debug.WriteLine(this, "Engine uninitialized");
        }



        void GPI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("GPI"+e.PropertyName);
        }

        public bool GPIConnected
        {
            get { return GPI != null && GPI.Connected; }
        }

        private bool _gPIEnabled = true;
        [XmlIgnore]
        public bool GPIEnabled
        {
            get { return _gPIEnabled; }
            set { SetField(ref _gPIEnabled, value, "GPIEnabled"); }
        }

        [XmlIgnore]
        public bool GPIAspectNarrow
        {
            get { return GPI != null && GPI.AspectNarrow; }
            set { if (GPI != null && _gPIEnabled) GPI.AspectNarrow = value; }
        }

        [XmlIgnore]
        public TCrawl GPICrawl
        {
            get { return GPI == null ? TCrawl.NoCrawl : (TCrawl)GPI.Crawl; }
            set { if (GPI != null && _gPIEnabled) GPI.Crawl = (int)value; }
        }

        [XmlIgnore]
        public TLogo GPILogo
        {
            get { return GPI == null ? TLogo.NoLogo : (TLogo)GPI.Logo; }
            set { if (GPI != null && _gPIEnabled) GPI.Logo = (int)value; }
        }

        [XmlIgnore]
        public TParental GPIParental
        {
            get { return GPI == null ? TParental.None : (TParental)GPI.Parental; }
            set { if (GPI != null && _gPIEnabled) GPI.Parental = (int)value; }
        }

        [XmlIgnore]
        public bool GPIIsMaster
        {
            get { return GPI != null && GPI.IsMaster; }
        }

        [XmlIgnore]
        public DateTime CurrentTime { get; private set; }

        public bool DateTimeEqal(DateTime dt1, DateTime dt2)
        {
            return AlignDateTime(dt1) == AlignDateTime(dt2);
        }

        public DateTime AlignDateTime(DateTime dt)
        {
            return new DateTime((dt.Ticks / _frameTicks) * _frameTicks, dt.Kind);
        }
        
        public TimeSpan AlignTimeSpan(TimeSpan ts)
        {
            return new TimeSpan((ts.Ticks / _frameTicks) * _frameTicks);
        }

        public override string ToString()
        {
            return EngineName;
        }

        private readonly SynchronizedCollection<Event> _allEvents = new SynchronizedCollection<Event>(); // list of all events loaded in engine

        [XmlIgnore]
        public readonly SynchronizedCollection<Event> RootEvents = new SynchronizedCollection<Event>();

        
        public bool AddEvent(Event aEvent)
        {
            lock (_allEvents.SyncRoot)
            {
                if (_allEvents.Contains(aEvent))
                    return false;
                else
                {
                    _allEvents.Add(aEvent);
                    aEvent.Saved += _eventSaved;
                    return true;
                }
            }
        }

        public enum ArchivePolicyType { NoArchive, ArchivePlayedAndNotUsedWhenDeleteEvent };

        public ArchivePolicyType ArchivePolicy;

        public bool RemoveEvent(Event aEvent)
        {
            RootEvents.Remove(aEvent);
            ServerMedia media = (ServerMedia)aEvent.Media;
            if (aEvent.PlayState == TPlayState.Played 
                && media != null 
                && media.MediaType == TMediaType.Movie 
                && ArchivePolicy == Engine.ArchivePolicyType.ArchivePlayedAndNotUsedWhenDeleteEvent
                && MediaManager.ArchiveDirectory != null
                && CanDeleteMedia(media).Reason == MediaDeleteDeny.MediaDeleteDenyReason.NoDeny)
                ThreadPool.QueueUserWorkItem(o => MediaManager.ArchiveMedia(media, true));
            if (_allEvents.Remove(aEvent))
            {
                aEvent.Saved -= _eventSaved;
                return true;
            };
            return false;
        }

        public void SaveAllEvents()
        {
            lock (_allEvents.SyncRoot)
                foreach (Event e in _allEvents)
                    e.Save();
        }
 
        private void _onServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var handler = ServerPropertyChanged;
            if (handler != null)
                handler(sender, e);
        }

        public Event PlayingEvent(VideoLayer layer = VideoLayer.Program)
        {
            return _visibleEvents[layer];
        }
             
        #region Preview Routines

        private ServerMedia _previewMedia;
        protected ServerMedia PreviewMedia { get { return _previewMedia; } }

        public void PreviewLoad(ServerMedia media, long seek, long duration, long position)
        {
            if (media != null)
            {
                _previewDuration = duration;
                _previewSeek = seek;
                _previewPosition = position;
                _previewMedia = media;
                PlayoutChannelPRV.Load(_previewMedia, VideoLayer.Preview, seek+position, duration-position);
                PreviewLoaded = true;
                PreviewIsPlaying = false;
                NotifyPropertyChanged("PreviewMedia");
                NotifyPropertyChanged("PreviewPosition");
                NotifyPropertyChanged("PreviewSeek");
            }
        }

        public void PreviewUnload()
        {
            var channel = PlayoutChannelPRV;
            if (channel != null)
            {
                if (_previewMedia != null)
                {
                    channel.Clear(VideoLayer.Preview);
                    _previewDuration = 0;
                    _previewPosition = 0;
                    _previewSeek = 0;
                    _previewMedia = null;
                    PreviewLoaded = false;
                    PreviewIsPlaying = false;
                    NotifyPropertyChanged("PreviewMedia");
                    NotifyPropertyChanged("PreviewPosition");
                    NotifyPropertyChanged("PreviewSeek");
                }
            }
        }

        private long _previewDuration;

        private long _previewPosition;

        private long _previewSeek;
        [XmlIgnore]
        public long PreviewSeek { get { return _previewSeek; } }

        [XmlIgnore]
        public long PreviewPosition // from 0 to duration
        {
            get { return _previewPosition; }
            set
            {
                if (PlayoutChannelPRV != null && _previewMedia!=null)
                {
                    PreviewPause();
                    long newSeek = value < 0 ? 0 : value;
                    long maxSeek = _previewDuration-1;
                    if (newSeek > maxSeek)
                        newSeek = maxSeek;
                    PlayoutChannelPRV.Load(_previewMedia, VideoLayer.Preview, _previewSeek + newSeek, _previewDuration - newSeek);
                    _previewPosition = newSeek;
                }
            }
        }

        public bool PreviewPlay()
        {
            var channel = PlayoutChannelPRV;
            var media = PreviewMedia;
            if (channel != null && channel.Play(VideoLayer.Preview) && media != null)
            {
                channel.SetVolume(VideoLayer.Preview, (decimal)Math.Pow(10, (double)media.AudioVolume / 20));
                PreviewIsPlaying = true;
                return true;
            }
            else
                return false;
        }

        public bool PreviewPause()
        {
            var channel = PlayoutChannelPRV;
            if (channel != null && channel.Pause(VideoLayer.Preview))
            {
                PreviewIsPlaying = false;
                return true;
            }
            else
                return false;
        }

        private bool _previewLoaded;

        [XmlIgnore]
        public bool PreviewLoaded {
            get { return _previewLoaded; }
            private set
            {
                if (value != _previewLoaded)
                {
                    _previewLoaded = value;
                    decimal vol = (_previewLoaded) ? 0 : _programAudioVolume;
                    if (PlayoutChannelPRV != null)
                        PlayoutChannelPRV.SetVolume(VideoLayer.Program, vol);
                }
            }
        }

        private bool _previewIsPlaying;
        [XmlIgnore]
        public bool PreviewIsPlaying { get { return _previewIsPlaying; } private set { SetField(ref _previewIsPlaying, value, "PreviewIsPlaying"); } }

        #endregion // Preview Routines

        private TEngineState _engineState;
        [XmlIgnore]
        public TEngineState EngineState
        {
            get { return _engineState; }
            private set
            {
                if (SetField(ref _engineState, value, "EngineState"))
                {
                    if (value == TEngineState.Hold)
                        foreach (Event ev in _runningEvents.Where(e => e.PlayState == TPlayState.Playing && e.Finished).ToList())
                        {
                            _pause(ev, true);
                            Debug.WriteLine(ev, "Hold: Played");
                        }
                    if (value == TEngineState.Idle && _runningEvents.Count>0)
                    {
                        foreach (Event ev in _runningEvents.Where(e => (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) && e.Finished).ToList())
                            {
                                _pause(ev, true);
                                Debug.WriteLine(ev, "Idle: Played");
                            }
                    }
                }
            }
        }

        private decimal _programAudioVolume = 1;
        [XmlIgnore]
        public decimal ProgramAudioVolume
        {
            get { return _programAudioVolume; }
            set 
            {
                if (value != _programAudioVolume)
                {
                    _programAudioVolume = value;
                    if (PlayoutChannelPGM != null)
                        PlayoutChannelPGM.SetVolume(VideoLayer.Program, _programAudioVolume);
                    if (PlayoutChannelPRV != null && !_previewLoaded)
                        PlayoutChannelPRV.SetVolume(VideoLayer.Program, _programAudioVolume);
                    NotifyPropertyChanged("ProgramAudioVolume");
                }
            }
        }

        public void ReScheduleAsync(Event aEvent)
        {
            ThreadPool.QueueUserWorkItem(o => ReSchedule(aEvent));
        }

        private object _rescheduleLock = new object();
        
        public void ReSchedule(Event aEvent)
        {
            lock (_rescheduleLock)
            {
                if (aEvent == null)
                    return;
                try
                {
                    if (aEvent.PlayState == TPlayState.Aborted
                        || aEvent.PlayState == TPlayState.Played
                        || !aEvent.Enabled)
                    {
                        aEvent.PlayState = TPlayState.Scheduled;
                        foreach (Event se in aEvent.SubEvents)
                            ReSchedule(se);
                        ReSchedule(aEvent.GetSuccessor());
                    }
                    else
                        aEvent.UpdateScheduledTime(true);
                }
                finally
                {
                    aEvent.Save();
                }
            }
        }

        private bool _load(Event aEvent)
        {
            if (aEvent != null && (!aEvent.Enabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetSuccessor();
            if (aEvent == null)
                return false;
            Debug.WriteLine(aEvent, "Load");
            if (aEvent.EventType != TEventType.Rundown)
            {
                if (PlayoutChannelPGM != null)
                    PlayoutChannelPGM.Load(aEvent);
                if (PlayoutChannelPRV != null)
                    PlayoutChannelPRV.Load(aEvent);
                _visibleEvents[aEvent.Layer] = aEvent;
                _finishedEvents[aEvent.Layer] = null;
                _loadedNextEvents[aEvent.Layer] = null;
                _setAspectRatio(aEvent);
            }
            aEvent.PlayState = TPlayState.Paused;
            foreach (Event se in (aEvent.SubEvents.Where(e => e.ScheduledDelay == TimeSpan.Zero)).ToList())
                _load(se);
            _run(aEvent);
            return true;
        }

        private bool _loadNext(Event aEvent)
        {
            if (aEvent != null && (!aEvent.Enabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetSuccessor();
            if (aEvent == null)
                return false;
            Debug.WriteLine(aEvent, "LoadNext");
            aEvent.PlayState = TPlayState.Scheduled;
            if (aEvent.EventType != TEventType.Rundown)
            {
                if (PlayoutChannelPGM != null)
                    PlayoutChannelPGM.LoadNext(aEvent);
                if (PlayoutChannelPRV != null)
                    PlayoutChannelPRV.LoadNext(aEvent);
                _loadedNextEvents[aEvent.Layer] = aEvent;
            }
            _run(aEvent);
            foreach (Event e in aEvent.SubEvents.ToList())
                if (e.ScheduledDelay.Ticks  < _frameTicks)
                    _loadNext(e);
            return true;
        }

        private bool _play(Event aEvent, bool fromBeginning)
        {
            if (aEvent != null && (!aEvent.Enabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetSuccessor();
            if (aEvent == null)
                return false;
            Debug.WriteLine("Play {1}: {0}", aEvent, CurrentTime.TimeOfDay);
            _run(aEvent);
            if (fromBeginning)
                aEvent.Position = 0;
            if (aEvent.EventType != TEventType.Rundown)
            {
                if (_visibleEvents[aEvent.Layer] != aEvent && !(_loadedNextEvents[aEvent.Layer] == aEvent)) 
                    _loadNext(aEvent);
                if (PlayoutChannelPGM != null)
                    PlayoutChannelPGM.Play(aEvent);
                if (PlayoutChannelPRV != null)
                    PlayoutChannelPRV.Play(aEvent);
                _loadedNextEvents[aEvent.Layer] = null;
                _finishedEvents[aEvent.Layer] = null;
                _visibleEvents[aEvent.Layer] = aEvent;
                if (aEvent.Layer == VideoLayer.Program)
                {
                    decimal volumeDB = (decimal)Math.Pow(10, (double)aEvent.GetAudioVolume() / 20);
                    ProgramAudioVolume = volumeDB;
                }
                _setAspectRatio(aEvent);
            }
            _triggerGPIGraphics(aEvent, true);
            aEvent.PlayState = TPlayState.Playing;

            foreach (Event e in aEvent.SubEvents.ToList())
                if (e.ScheduledDelay.Ticks < _frameTicks)
                    _play(e, fromBeginning);

            aEvent.Save();
            if (_pst2Prv)
                _loadPST();
            NotifyEngineOperation(aEvent, TEngineOperation.Play);
            if (aEvent.Layer == VideoLayer.Program
                && (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.Live))
                ThreadPool.QueueUserWorkItem(new WaitCallback(o => aEvent.AsRunLogWrite()));
            return true;
        }

        private void _clearRunning()
        {
            Debug.WriteLine("_clearRunning");
            foreach (Event e in _runningEvents.ToList())
            {
                if (e.PlayState == TPlayState.Playing)
                    e.PlayState = TPlayState.Aborted;
                if (e.PlayState == TPlayState.Fading)
                    e.PlayState = TPlayState.Played;
                if (e.PlayState == TPlayState.Paused)
                    e.PlayState = TPlayState.Scheduled;
            }
            _runningEvents.Clear();
        }

        private void _setAspectRatio(Event aEvent)
        {
            if (aEvent == null || !(aEvent.Layer == VideoLayer.Program || aEvent.Layer == VideoLayer.Preset))
                return;
            Media media = aEvent.Media;
            bool narrow = media != null && (media.VideoFormat == TVideoFormat.PAL || media.VideoFormat == TVideoFormat.NTSC);
            if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
            {
                if (PlayoutChannelPGM != null)
                    PlayoutChannelPGM.SetAspect(narrow);
                if (PlayoutChannelPRV != null)
                    PlayoutChannelPRV.SetAspect(narrow);
            }
            if (AspectRatioControl == TAspectRatioControl.GPI || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                if (GPI != null)
                    GPI.AspectNarrow = narrow;
        }

        public void Load(Event aEvent)
        {
            Debug.WriteLine(aEvent, "Load");
            lock (_tickLock)
            {
                EngineState = TEngineState.Hold;
                IEnumerable<Event> oldEvents = _visibleEvents.Values.ToList().Concat(_finishedEvents.Values.ToList());
                _visibleEvents.Clear();
                _finishedEvents.Clear();
                foreach (Event e in _runningEvents.ToList())
                {
                    _runningEvents.Remove(e);
                    if (e.Position == 0)
                        e.PlayState = TPlayState.Scheduled;
                    else
                        e.PlayState = TPlayState.Aborted;
                }
                _load(aEvent);
                foreach (Event e in oldEvents)
                    if (_visibleEvents[e.Layer] == null)
                        Clear(e.Layer);
            }
            NotifyEngineOperation(aEvent, TEngineOperation.Load);
        }

        public void StartLoaded()
        {
            Debug.WriteLine("StartLoaded executed");
            lock (_tickLock)
                if (EngineState == TEngineState.Hold)
                {
                    foreach (Event e in _runningEvents.ToList())
                    {
                        if (e.PlayState == TPlayState.Paused || _loadedNextEvents.Values.Contains(e))
                        {
                            _play(e, false);
                            Event s = e.GetSuccessor();
                            if (s != null)
                                s.UpdateScheduledTime(true);
                        }
                    }
                    EngineState = TEngineState.Running;
                }
        }

        public void Start(Event aEvent)
        {
            Debug.WriteLine(aEvent, "Start");
            lock (_tickLock)
            {
                EngineState = TEngineState.Running;
                IEnumerable<Event> oldEvents = _visibleEvents.Values.ToList().Concat(_finishedEvents.Values.ToList());
                _visibleEvents.Clear();
                _finishedEvents.Clear();
                foreach (Event e in _runningEvents.ToList())
                {
                    _runningEvents.Remove(e);
                    if (e.Position == 0)
                        e.PlayState = TPlayState.Scheduled;
                    else
                        e.PlayState = TPlayState.Aborted;
                }

                _play(aEvent, true);
                foreach (Event e in oldEvents)
                    if (_visibleEvents[e.Layer] == null)
                        Clear(e.Layer);
            }
            NotifyEngineOperation(aEvent, TEngineOperation.Start);
        }

        public void Schedule(Event aEvent)
        {
            Debug.WriteLine(aEvent, string.Format("Schedule {0}", aEvent.PlayState));
            lock (_tickLock)
                EngineState = TEngineState.Running;
            _run(aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Schedule);
        }

        private void _run(Event aEvent)
        {
            if (aEvent == null)
                return;
            lock (_tickLock)
            {
                if (!_runningEvents.Contains(aEvent))
                {
                    aEvent.UpdateScheduledTime(false);
                    aEvent._startTime = default(DateTime);
                    _runningEvents.Add(aEvent);
                }
            }
        }

        private void _stop(Event aEvent)
        {
            aEvent.PlayState = TPlayState.Played;
            aEvent.Save();
            lock (_visibleEvents)
                if (_visibleEvents[aEvent.Layer] == aEvent)
                {
                    var le = _loadedNextEvents[aEvent.Layer];
                    if (aEvent.EventType != TEventType.Live
                        && (le == null
                        || (le.ScheduledTime.Ticks - CurrentTicks >= _frameTicks)))
                    {
                        Debug.WriteLine(aEvent, "Stop");
                        if (PlayoutChannelPGM != null)
                            PlayoutChannelPGM.Stop(aEvent);
                        if (PlayoutChannelPRV != null)
                            PlayoutChannelPRV.Stop(aEvent);
                    }
                    _visibleEvents[aEvent.Layer] = null;
                }
            _runningEvents.Remove(aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Stop);
        }

        private void _pause(Event aEvent, bool finish)
        {
            lock (_visibleEvents)
                if (_visibleEvents[aEvent.Layer] == aEvent)
                {
                    Debug.WriteLine(aEvent, "Pause");
                    if (aEvent.EventType != TEventType.Live)
                    {
                        if (PlayoutChannelPGM != null)
                            PlayoutChannelPGM.Pause(aEvent);
                        if (PlayoutChannelPRV != null)
                            PlayoutChannelPRV.Pause(aEvent);
                    }
                    if (finish)
                    {
                        _visibleEvents[aEvent.Layer] = null;
                        _finishedEvents[aEvent.Layer] = aEvent;
                    }
                    foreach (Event se in aEvent.SubEvents.ToList())
                        _pause(se, finish);
                }
            if (finish)
            {
                aEvent.PlayState = TPlayState.Played;
                aEvent.Save();
                _runningEvents.Remove(aEvent);
                NotifyEngineOperation(aEvent, TEngineOperation.Stop);
            }
            else
                NotifyEngineOperation(aEvent, TEngineOperation.Pause);
        }

        private void _loadPST()
        {
            var currEvent = PlayingEvent();
            if (PlayoutChannelPRV != null)
                if (currEvent != null)
                {
                    var nextEvent = currEvent.GetSuccessor();
                    if (nextEvent != null)
                    {
                        var media = nextEvent.ServerMediaPRV;
                        if (media != null)
                        {
                            PlayoutChannelPRV.Load(media, VideoLayer.Preset, 0, -1);
                            return;
                        }
                    }
                }
                else
                    PlayoutChannelPRV.Load(System.Drawing.Color.Black, VideoLayer.Preset);
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            Debug.WriteLine(aVideoLayer, "Clear");
            Event ev;
            _loadedNextEvents.TryRemove(aVideoLayer, out ev);
            if (ev != null)
            {
                ev.PlayState = TPlayState.Scheduled;
                ev.Save();
                _runningEvents.Remove(ev);
            }
            _visibleEvents.TryRemove(aVideoLayer, out ev);
            if (ev != null)
            {
                ev.PlayState = TPlayState.Aborted;
                ev.Save();
                _runningEvents.Remove(ev);
            }
            _finishedEvents.TryRemove(aVideoLayer, out ev);
            if (ev != null)
            {
                ev.PlayState = TPlayState.Scheduled;
                ev.Save();
                _runningEvents.Remove(ev);
            }
            if (PlayoutChannelPGM != null)
                PlayoutChannelPGM.Clear(aVideoLayer);
            if (PlayoutChannelPRV != null)
                PlayoutChannelPRV.Clear(aVideoLayer);
        }
        
        public void Clear()
        {
            _clearRunning();
            foreach (Event e in _loadedNextEvents.Values.ToList())
                e.PlayState = TPlayState.Scheduled;
            _visibleEvents.Clear();
            _loadedNextEvents.Clear();
            _finishedEvents.Clear();
            PreviewUnload(); 
            if (PlayoutChannelPGM != null)
                PlayoutChannelPGM.Clear();
            if (PlayoutChannelPRV != null)
                PlayoutChannelPRV.Clear();
            NotifyEngineOperation(null, TEngineOperation.Clear);
            _programAudioVolume = 1.0m;
            NotifyPropertyChanged("ProgramAudioVolume");
            lock (_tickLock)
                EngineState = TEngineState.Idle;
        }

        public void RestartLayer(VideoLayer aLayer)
        {
            if (PlayoutChannelPGM != null)
                PlayoutChannelPGM.ReStart(aLayer);
            if (PlayoutChannelPRV != null)
                PlayoutChannelPRV.ReStart(aLayer);
        }

        //private void _reRun(Event aEvent)
        //{
        //    Debug.WriteLine(aEvent, string.Format("ReRun {0}", aEvent.PlayState));
        //}

        public void RestartRundown(Event ARundown)
        {
            Action<Event> _rerun = (aEvent) =>
                {
                    if (!_runningEvents.Contains(aEvent))
                        _runningEvents.Add(aEvent);
                    if (aEvent.EventType != TEventType.Rundown)
                    {
                        _visibleEvents[aEvent.Layer] = aEvent;
                        RestartLayer(aEvent.Layer);
                    }
                };

            Event ev = ARundown;
            while (ev != null)
            {
                if (CurrentTicks >= ev.ScheduledTime.Ticks && CurrentTicks < ev.ScheduledTime.Ticks + ev.Duration.Ticks)
                {
                    ev.Position = (CurrentTicks - ev.ScheduledTime.Ticks) / FrameTicks;
                    var st = ev.StartTime;
                    ev.PlayState = TPlayState.Playing;
                    if (st != ev.StartTime)
                        ev.StartTime = st;
                    _rerun(ev);
                    foreach (Event se in ev.SubEvents)
                        RestartRundown(se);
                    break;
                }
                else
                    ev = ev.GetSuccessor();
            }
            lock (_tickLock)
                EngineState = TEngineState.Running;
            NotifyEngineOperation(ARundown, TEngineOperation.Start);
        }

        private object _tickLock = new object();
        private void _tick(long nFrames)
        {
            lock (_tickLock)
            {
                if (EngineState == TEngineState.Running)
                {
                    IEnumerable<Event> runningEvents = null;
                    lock (_runningEvents.SyncRoot)
                        runningEvents = _runningEvents.ToList();
                    foreach (Event e in runningEvents.Where(e => e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading))
                        e.Position += nFrames;

                    if (runningEvents.Any(e => CurrentTicks >= e.ScheduledTime.Ticks
                                                && e.PlayState == TPlayState.Scheduled
                                                && e.Hold))
                    {
                        EngineState = TEngineState.Hold;
                        return;
                    }

                    foreach (Event ev in runningEvents)
                    {
                        Event succ = ev.GetSuccessor();

                        _triggerGPIGraphics(ev, false);
                        _triggerGPIGraphics(succ, false);

                        // first: check if some events should finish
                        if (ev.PlayState == TPlayState.Playing || ev.PlayState == TPlayState.Fading)
                        {
                            if (ev.Finished)
                                _stop(ev);
                            if (succ != null
                                && ev.Position * _frameTicks >= (ev.Length.Ticks + succ.ScheduledDelay.Ticks - succ.TransitionTime.Ticks))
                            {
                                if (ev.PlayState == TPlayState.Playing)
                                {
                                    ev.PlayState = TPlayState.Fading;
                                    Debug.WriteLine(ev, "Tick: Fading");
                                }
                            }
                            if (succ != null
                                && CurrentTicks >= succ.ScheduledTime.Ticks - _preloadTime.Ticks
                                && !_runningEvents.Contains(succ))
                            {
                                // second: preload next scheduled events
                                Debug.WriteLine(succ, "Tick: LoadNext Running");
                                succ.Position = 0;
                                _loadNext(succ);
                            }
                        }

                        // third: start 
                        if (!ev.Hold
                            && CurrentTicks >= ev.ScheduledTime.Ticks
                            && ev.PlayState == TPlayState.Scheduled)
                        {
                            if (CurrentTicks >= ev.ScheduledTime.Ticks + ev.ScheduledDelay.Ticks)
                            {
                                Debug.WriteLine(ev, string.Format("Tick: Play current time: {0} scheduled time: {1}", CurrentTime, ev.ScheduledTime + ev.ScheduledDelay));
                                _play(ev, true);
                            }
                        }

                        lock (_runningEvents.SyncRoot)
                            if (!_runningEvents.Any(e => !e.Finished))
                            {
                                EngineState = TEngineState.Idle;
                                return;
                            }
                    }
                }
                
                // preview controls
                if (PreviewIsPlaying)
                {
                    if (_previewPosition < _previewDuration - 1)
                    {
                        if (nFrames > 0)
                        {
                            _previewPosition += nFrames;
                            NotifyPropertyChanged("PreviewPosition");
                        }
                    }
                    else
                        PreviewPause();
                }
            }
        }

        private void _triggerGPIGraphics(Event ev, bool ignoreScheduledTime) // aspect is triggered on _play
        {
            if (!this.GPIEnabled
                || ev == null
                || !ev.GPI.CanTrigger)
                return;
            if (GPI != null
                && !ev.GPITrigerred
                && (ignoreScheduledTime ||( !ev.Hold && CurrentTicks >= ev.ScheduledTime.Ticks + ev.ScheduledDelay.Ticks + GPI.GraphicsStartDelay * 10000L )))
            {
                ev.GPITrigerred = true;
                GPI.Crawl = (int)ev.GPI.Crawl;
                GPI.Logo = (int)ev.GPI.Logo;
                GPI.Parental = (int)ev.GPI.Parental;
            }
            if (LocalGpi != null
                && !ev.LocalGPITriggered
                && (ignoreScheduledTime || ( !ev.Hold && CurrentTicks >= ev.ScheduledTime.Ticks + ev.ScheduledDelay.Ticks )))
            {
                ev.LocalGPITriggered = true;
                LocalGpi.Crawl = (int)ev.GPI.Crawl;
                LocalGpi.Logo = (int)ev.GPI.Logo;
                LocalGpi.Parental = (int)ev.GPI.Parental;
            }
        }
        

        private MediaDeleteDeny _checkCanDeleteMedia(Event ev, ServerMedia media)
        {
            Event nev = ev;
            while (nev != null)
            {
                if (nev.EventType == TEventType.Movie 
                    && nev.Media == media 
                    && nev.ScheduledTime >= CurrentTime)
                    return new MediaDeleteDeny() { Reason = MediaDeleteDeny.MediaDeleteDenyReason.MediaInFutureSchedule, Event = nev, Media = media };
                foreach (Event se in nev.SubEvents)
                {
                    MediaDeleteDeny reason = _checkCanDeleteMedia(se, media);
                    if (reason.Reason != MediaDeleteDeny.MediaDeleteDenyReason.NoDeny)
                        return reason;
                }
                nev = nev.Next;
            }
            return MediaDeleteDeny.NoDeny;
        }

        private void _mediaPGMVerified(object o, MediaEventArgs e)
        {
            if (PlayoutChannelPRV != null
                && PlayoutChannelPRV.OwnerServer != PlayoutChannelPGM.OwnerServer
                && PlayoutChannelPRV.OwnerServer.MediaDirectory.IsInitialized)
            {
                Media media = PlayoutChannelPRV.OwnerServer.MediaDirectory.GetServerMedia(e.Media, true);
                if (media.FileSize == e.Media.FileSize
                    && media.FileName == e.Media.FileName
                    && media.FileSize == e.Media.FileSize
                    && !media.Verified)
                    media.Verify();
                if (!(media.MediaStatus == TMediaStatus.Available
                      || media.MediaStatus == TMediaStatus.Copying
                      || media.MediaStatus == TMediaStatus.CopyPending
                      || media.MediaStatus == TMediaStatus.Copied))
                    FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Copy, SourceMedia = e.Media, DestMedia = media });
            }
        }

        private void _mediaPGMRemoved(object o, MediaEventArgs e)
        {
            if (PlayoutChannelPRV != null && !e.Media.FileExists())
            {
                Media media = PlayoutChannelPRV.OwnerServer.MediaDirectory.FindMedia(e.Media);
                if (media != null && media.MediaStatus == TMediaStatus.Available)
                    FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = media });
            }
        }

        internal MediaDeleteDeny CanDeleteMedia(ServerMedia serverMedia)
        {
            MediaDeleteDeny reason = MediaDeleteDeny.NoDeny;
            foreach (Event e in RootEvents.ToList())
            {
                reason = _checkCanDeleteMedia(e, serverMedia);
                if (reason.Reason != MediaDeleteDeny.MediaDeleteDenyReason.NoDeny)
                    return reason;
            }
            return serverMedia.DbMediaInUse();
        }

        [XmlIgnore]
        public IEnumerable<Event> VisibleEvents { get { return _visibleEvents.Values; } }
        [XmlIgnore]
        public IEnumerable<Event> LoadedNextEvents { get { return _loadedNextEvents.Values; } }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        private void _onRunningSubEventsChanged(object sender, CollectionOperationEventArgs<Event> e)
        {
            if (e.Operation == TCollectionOperation.Remove)
                _stop(e.Item);
            else
            {
                lock (_tickLock)
                {
                    TPlayState ps = ((Event)sender).PlayState;
                    if (ps == TPlayState.Playing || ps == TPlayState.Paused)
                    {
                        e.Item.Position = ((Event)sender).Position;
                        _play(e.Item, false);
                    }
                }
            }
        }

        public TimeSpan GetTimeToAttention()
        {
            Event pe = PlayingEvent();
            if (pe != null)
            {
                TimeSpan result = pe.Length - TimeSpan.FromTicks(pe.Position * _frameTicks);
                pe = pe.GetSuccessor();
                while (pe != null)
                {
                    TimeSpan? pauseTime = pe.GetAttentionTime();
                    if (pauseTime != null)
                        return result + pauseTime.Value - pe.TransitionTime;
                    result = result + pe.Length - pe.TransitionTime;
                    pe = pe.GetSuccessor();
                }
                return result;
            }
            return TimeSpan.Zero;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyEngineOperation(Event aEvent, TEngineOperation operation)
        {
            var handler = EngineOperation; 
            if (handler != null)
                handler(this, new EngineOperationEventArgs(aEvent, operation));
        }

        public event EventHandler<DictionaryOperationEventArgs<VideoLayer, Event>> VisibleEventsOperation;
        private void _visibleEventsOperation(object o, DictionaryOperationEventArgs<VideoLayer, Event> e)
        {
            var handler = VisibleEventsOperation;
            if (handler != null)
                handler(o, e);
        }

        public event EventHandler<DictionaryOperationEventArgs<VideoLayer, Event>> LoadedNextEventsOperation;
        private void _loadedNextEventsOperation(object o, DictionaryOperationEventArgs<VideoLayer, Event> e)
        {
            var handler = LoadedNextEventsOperation;
            if (handler != null)
                handler(o, e);
        }

        public event EventHandler<CollectionOperationEventArgs<Event>> RunningEventsOperation;
        private void _runningEventsOperation(object sender, CollectionOperationEventArgs<Event> e)
        {
            if (e.Operation == TCollectionOperation.Insert)
                e.Item.SubEventChanged += _onRunningSubEventsChanged;
            if (e.Operation == TCollectionOperation.Remove)
                e.Item.SubEventChanged -= _onRunningSubEventsChanged;
            var handler = RunningEventsOperation;
            if (handler  != null)
                handler(sender, e);
        }

        public event EventHandler EventSaved; 
        private void _eventSaved(object sender, EventArgs e)
        {
            var handler = EventSaved;
            if (handler != null)
                handler(sender, e);
        }

        internal void SearchMissingEvents()
        {
            this.DbSearchMissing();
        }

        private bool _pst2Prv;
        [XmlIgnore]
        public bool Pst2Prv
        {
            get { return _pst2Prv; }
            set
            {
                if (SetField(ref _pst2Prv, value, "Pst2Prv"))
                {
                    if (value)
                        _loadPST();
                    else
                    {
                        if (PlayoutChannelPRV != null)
                            PlayoutChannelPRV.Clear(VideoLayer.Preset);
                    }
                }
            }
        }

    }

    public class EngineOperationEventArgs : EventArgs
    {
        public EngineOperationEventArgs(Event AEvent, TEngineOperation AOperation)
        {
            Operation = AOperation;
            Event = AEvent;
        }
        public TEngineOperation Operation { get; private set; }
        public Event Event { get; private set; }
    }
}