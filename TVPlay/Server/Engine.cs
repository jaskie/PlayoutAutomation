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
    
    public class Engine : INotifyPropertyChanged, IDisposable, IEngine
    {
        [XmlIgnore]
        public UInt64 Id { get; set; }
        [XmlIgnore]
        public UInt64 Instance { get; internal set; }

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
        internal UInt64 idArchive;

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
        public GPINotifier GPI;
             
        public Remoting.RemoteHost Remote;
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

        long _frameDuration; // in nanoseconds
        long _frameTicks;
        public long FrameTicks { get { return _frameTicks; } }
        public TVideoFormat VideoFormat { get; set; }

        public void Initialize(IGpi localGpi)
        {
            Debug.WriteLine(this, "Begin initializing");
            LocalGpi = localGpi;
            switch (VideoFormat)
            {
                case TVideoFormat.HD1080p5000:
                case TVideoFormat.HD720p5000:
                    _frameDuration = 20000000L;
                    break;
                case TVideoFormat.NTSC:
                case TVideoFormat.HD1080p3000:
                case TVideoFormat.HD1080i6000:
                case TVideoFormat.HD2160p3000:
                    _frameDuration = 33300000L;
                    break;
                case TVideoFormat.HD1080p6000:
                case TVideoFormat.HD720p6000:
                    _frameDuration = 16650000L;
                    break;
                case TVideoFormat.HD1080i5994:
                case TVideoFormat.HD1080p2997:
                case TVideoFormat.HD2160p2997:
                    _frameDuration = 33366700L;
                    break;
                case TVideoFormat.HD1080p5994:
                case TVideoFormat.HD720p5994:
                    _frameDuration = 16683350L; 
                    break;
                case TVideoFormat.HD1080p2398:
                case TVideoFormat.HD2160p2398:
                    _frameDuration = 41701418L;
                    break;
                case TVideoFormat.HD2160p2400:
                case TVideoFormat.HD1080p2400:
                    _frameDuration = 41666667L;
                    break;
                default:
                    _frameDuration = 40000000L; //ns, PAL
                    break;
            }
            _frameTicks = _frameDuration / 100L;

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
                        long nFrames = (CurrentTime.Ticks - CurrentTicks) * 100L / _frameDuration;
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
                    long timeToWait = (_frameDuration - 100L * (DateTime.UtcNow.Ticks + _timeCorrection.Ticks - CurrentTicks)) / 1000000L;
                    if (timeToWait > 0)
                       Thread.Sleep((int)timeToWait);
                }
                Debug.WriteLine(this, "Engine thread finished");
            });
            _engineThread.Priority = ThreadPriority.Highest;
            _engineThread.Name = string.Format("Engine main thread for {0}", EngineName);
            _engineThread.IsBackground = true;
            _engineThread.Start();
            EngineState = TEngineState.Idle;

            var gpi = GPI;
            if (gpi != null)
            {
                Debug.WriteLine(this, "Initializing GPI");
                gpi.Started += Resume;
                gpi.Initialize();
                gpi.PropertyChanged += GPI_PropertyChanged;
            }

            if (Remote != null)
            {
                Debug.WriteLine(this, "Initializing Remote interface");
                Remote.Initialize(this);
            }

            if (localGpi != null)
                localGpi.Started += Resume;

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
                localGpi.Started -= Resume;

            var gpi = GPI;
            if (gpi != null)
            {
                Debug.WriteLine(this, "Uninitializing GPI");
                gpi.Started -= Resume;
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
                && CanDeleteMedia(media))
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
            if (channel != null && channel.Play(VideoLayer.Preview))
            {
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
                    decimal vol = (_previewLoaded) ? 0 : _audioVolume;
                    if (PlayoutChannelPRV != null)
                        PlayoutChannelPRV.SetVolume(VideoLayer.Program, vol);
                    NotifyPropertyChanged("PreviewIsPlaying");
                }
            }
        }

        [XmlIgnore]
        public bool PreviewIsPlaying { get; private set; }

        #endregion // Preview Routines

        private TEngineState _engineState;
        
        [XmlIgnore]
        public TEngineState EngineState
        {
            get { return _engineState; }
            private set { SetField(ref _engineState, value, "EngineState"); }
        }

        private decimal _audioVolume = 1;
        
        [XmlIgnore]
        public decimal AudioVolume
        {
            get { return _audioVolume; }
            set 
            {
                if (value != _audioVolume)
                {
                    _audioVolume = value;
                    if (PlayoutChannelPGM != null)
                        PlayoutChannelPGM.SetVolume(VideoLayer.Program, _audioVolume);
                    if (PlayoutChannelPRV != null && !_previewLoaded)
                        PlayoutChannelPRV.SetVolume(VideoLayer.Program, _audioVolume);
                    NotifyPropertyChanged("AudioVolume");
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
                        ReSchedule(aEvent.Successor);
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
            while (aEvent != null && (!aEvent.Enabled || aEvent.Length == TimeSpan.Zero))
            {
                if (!aEvent.Enabled)
                {
                    _runningEvents.Remove(aEvent);
                    aEvent = aEvent.Successor;
                }
                if (aEvent.Length == TimeSpan.Zero)
                {
                    _runningEvents.Remove(aEvent);
                    aEvent.PlayState = TPlayState.Played;
                    aEvent = aEvent.Successor;
                }
            }
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
            while (aEvent != null && (!aEvent.Enabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.Successor;
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
            while (aEvent != null && (!aEvent.Enabled || aEvent.Length == TimeSpan.Zero))
            {
                if (!aEvent.Enabled)
                {
                    _runningEvents.Remove(aEvent);
                    aEvent = aEvent.Successor;
                }
                if (aEvent.Length == TimeSpan.Zero)
                {
                    _runningEvents.Remove(aEvent);
                    aEvent.PlayState = TPlayState.Played;
                    aEvent = aEvent.Successor;
                }
            }
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
                    decimal volumeDB = (decimal)Math.Pow(10, (double)aEvent.AudioVolume / 20);
                    AudioVolume = volumeDB;
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
                aEvent.AsRunLogWrite();
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

        public void Resume()
        {
            Debug.WriteLine("Resume executed");
            lock (_tickLock)
                if (EngineState == TEngineState.Hold)
                {
                    foreach (Event e in _visibleEvents.Values.ToList())
                    {
                        _play(e, false);
                        Event s = e.Successor;
                        if (s != null)
                            s.UpdateScheduledTime(true);
                    }
                    foreach (Event e in _runningEvents.ToList())
                        e.PlayState = TPlayState.Playing;
                    EngineState = TEngineState.Running;
                }
        }

        public void Pause()
        {
            lock (_tickLock)
                if (EngineState == TEngineState.Running)
                {
                    EngineState = TEngineState.Hold;
                    foreach (Event e in _visibleEvents.Values.ToList())
                    {
                        _pause(e, false);
                    }
                }
        }
        

        public void Seek(Event aEvent, long position)
        {
            if (aEvent != null && aEvent.Media != null && _visibleEvents[aEvent.Layer] == aEvent)
            {
                Debug.WriteLine(aEvent, "Stop");
                if (PlayoutChannelPGM != null)
                    PlayoutChannelPGM.Load(aEvent.ServerMediaPGM, aEvent.Layer, position, -1);
                if (PlayoutChannelPRV != null)
                    PlayoutChannelPRV.Load(aEvent.ServerMediaPRV, aEvent.Layer, position, -1);
                long dif = position - aEvent.Position;
                aEvent.Position = position;
                foreach (Event e in aEvent.SubEvents.ToList())
                    e.Position += dif;
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
                    var nextEvent = currEvent.Successor;
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
            _audioVolume = 1.0m;
            NotifyPropertyChanged("AudioVolume");
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
                    ev = ev.Successor;
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
                IEnumerable<Event> runningEvents = null;
                lock (_runningEvents.SyncRoot)
                    runningEvents = _runningEvents.ToList();
                if (EngineState == TEngineState.Running && runningEvents != null)
                {
                    if (runningEvents.Count() == 0)
                        EngineState = TEngineState.Idle;
                    foreach (Event ev in runningEvents)
                        if (CurrentTicks >= ev.ScheduledTime.Ticks
                            && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused)
                            && ev.Hold)
                        {
                            EngineState = TEngineState.Hold;
                            foreach (Event e in runningEvents)
                            {
                                if (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading)
                                {
                                    e.Position += nFrames;             //increase position in playing files
                                    if (e.Position * _frameTicks >= e.Length.Ticks)
                                    {
                                        Debug.WriteLine(e, "Hold: Played");
                                        _pause(e, true);
                                    }
                                }
                            }
                        }

                    if (EngineState != TEngineState.Hold)
                    {
                        foreach (Event e in runningEvents.Where(e => e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading))
                            e.Position += nFrames;
                        bool isEndOfRundown = !runningEvents.Any(e => e.Position * _frameTicks < e.Length.Ticks);
                        if (isEndOfRundown)
                            EngineState = TEngineState.Hold;
                        foreach (Event ev in runningEvents)
                        {
                            Event succ = ev.Successor;
                            while (succ != null && (!succ.Enabled || succ.Length == TimeSpan.Zero))
                                succ = succ.Successor;

                            _triggerGPIGraphics(ev, false);
                            _triggerGPIGraphics(succ, false);

                            // first: check if some events should finish
                            if (ev.PlayState == TPlayState.Playing || ev.PlayState == TPlayState.Fading)
                            {
                                if (ev.Position * _frameTicks >= ev.Length.Ticks)
                                {
                                    Debug.WriteLine(ev, "Tick: Played");
                                    if (isEndOfRundown)
                                        _pause(ev, true);
                                    else
                                        _stop(ev);
                                }
                                if (succ != null
                                    && ev.Position * _frameTicks >= (ev.Length.Ticks + succ.ScheduledDelay.Ticks - succ.TransitionTime.Ticks))
                                {
                                    if (ev.PlayState == TPlayState.Playing)
                                    {
                                        ev.PlayState = TPlayState.Fading;
                                        Debug.WriteLine(ev, "Tick: Fading");
                                    }
                                }
                                if (CurrentTicks >= ev.EndTime.Ticks - _preloadTime.Ticks)
                                {
                                    // second: preload next scheduled events
                                    if (succ != null)
                                    {
                                        if (!_runningEvents.Contains(succ)
                                        && CurrentTicks >= succ.ScheduledTime.Ticks - _preloadTime.Ticks)
                                        {
                                            Debug.WriteLine(succ, "Tick: LoadNext Running");
                                            succ.Position = 0;
                                            _loadNext(succ);
                                        }
                                    }
                                }
                            }

                            // third: start 
                            if (!ev.Hold
                                && CurrentTicks >= ev.ScheduledTime.Ticks
                                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused))
                            {
                                if (CurrentTicks >= ev.ScheduledTime.Ticks + ev.ScheduledDelay.Ticks)
                                {
                                    Debug.WriteLine(ev, string.Format("Tick: Play current time: {0} scheduled time: {1}", CurrentTime, ev.ScheduledTime + ev.ScheduledDelay));
                                    _play(ev, true);
                                }
                            }
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
                && (ignoreScheduledTime || CurrentTicks >= ev.ScheduledTime.Ticks + ev.ScheduledDelay.Ticks + GPI.GraphicsStartDelay * 10000L))
            {
                ev.GPITrigerred = true;
                GPI.Crawl = (int)ev.GPI.Crawl;
                GPI.Logo = (int)ev.GPI.Logo;
                GPI.Parental = (int)ev.GPI.Parental;
            }
            if (LocalGpi != null
                && !ev.LocalGPITriggered
                && (ignoreScheduledTime || CurrentTicks >= ev.ScheduledTime.Ticks + ev.ScheduledDelay.Ticks))
            {
                ev.LocalGPITriggered = true;
                LocalGpi.Crawl = (int)ev.GPI.Crawl;
                LocalGpi.Logo = (int)ev.GPI.Logo;
                LocalGpi.Parental = (int)ev.GPI.Parental;
            }
        }
        

        private bool _checkCanDeleteMedia(Event ev, ServerMedia media)
        {
            Event nev = ev;
            while (nev != null)
            {
                if (nev.EventType == TEventType.Movie 
                    && nev.Media == media 
                    && nev.ScheduledTime >= CurrentTime)
                    return false;
                foreach (Event se in nev.SubEvents)
                    if (!_checkCanDeleteMedia(se, media))
                        return false;
                nev = nev.Next;
            }
            return true;
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

        internal bool CanDeleteMedia(ServerMedia serverMedia)
        {
            foreach (Event e in RootEvents.ToList())
                if (!_checkCanDeleteMedia(e, serverMedia))
                    return false;
            return !serverMedia.DbMediaInUse();
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
                    e.Item.Position = ((Event)sender).Position;
                    _play(e.Item, false);
                }
            }
        }

        [XmlIgnore]
        public TimeSpan TimeToPause
        {
            get
            {
                Event pe = PlayingEvent();
                if (pe != null)
                {
                    long result = -pe.Position+pe.LengthInFrames;
                    pe = pe.Successor;
                    while (pe != null && !pe.Hold && !(pe.EventType==TEventType.Live))
                    {
                        result = result + pe.LengthInFrames;
                        pe = pe.Successor;
                    }
                    return new TimeSpan(result * _frameTicks);
                }
                return TimeSpan.Zero;
            }
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