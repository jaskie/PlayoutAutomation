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
using TAS.Server.Common;

namespace TAS.Server
{
    
    public class Engine : IEngine, IDisposable
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

        private readonly IMediaManager _mediaManager;
        [XmlIgnore]
        public IMediaManager MediaManager { get { return _mediaManager; } }
        [XmlIgnore]
        public IGpi LocalGpi { get; private set; }

        Thread _engineThread;
        internal long CurrentTicks;
        
        private TimeSpan _preloadTime = new TimeSpan(0, 0, 2); // time to preload event
        internal SimpleDictionary<VideoLayer, IEvent> _visibleEvents = new SimpleDictionary<VideoLayer, IEvent>(); // list of visible events
        internal ObservableSynchronizedCollection<IEvent> _runningEvents = new ObservableSynchronizedCollection<IEvent>(); // list of events loaded and playing 
        internal SimpleDictionary<VideoLayer, IEvent> _loadedNextEvents = new SimpleDictionary<VideoLayer, IEvent>(); // events loaded in backgroud
        private SimpleDictionary<VideoLayer, IEvent> _finishedEvents = new SimpleDictionary<VideoLayer, IEvent>(); // events finished or loaded and not playing

        public event EventHandler<EngineTickEventArgs> EngineTick;
        public event EventHandler<EngineOperationEventArgs> EngineOperation;
        public event EventHandler<PropertyChangedEventArgs> ServerPropertyChanged;
        
        [XmlElement("Gpi")]
        public GPINotifier _serGpi { get { return null; } set { _gpi = value; } }
        private GPINotifier _gpi;
        public IGpi Gpi { get { return _gpi; } }

        public RemoteHost Remote { get; set; }
        public TAspectRatioControl AspectRatioControl { get; set; }
        public double VolumeReferenceLoudness { get; set; }

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
            _mediaManager = new MediaManager(this);
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
                foreach (Event e in _rootEvents)
                    e.SaveLoadedTree();
                if (_gpi != null)
                    _gpi.Dispose();
                var remote = Remote;
                if (remote != null)
                    remote.Dispose();
            }
        }

#endregion //IDisposable

        private IPlayoutServerChannel _playoutChannelPGM;

        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPGM
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
                        old.Engine = null;
                    }
                    _playoutChannelPGM = value;
                    if (value != null)
                    {
                        value.OwnerServer.PropertyChanged += _onServerPropertyChanged;
                        value.Engine = this;
                    }
                    NotifyPropertyChanged("PlayoutChannelPGM");
                }
            }
        }
        private IPlayoutServerChannel _playoutChannelPRV;
        
        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPRV
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
        RationalNumber _frameRate;
        [XmlIgnore]
        public RationalNumber FrameRate { get { return _frameRate; } }

        public TVideoFormat VideoFormat { get; set; }

        [XmlIgnore]
        public VideoFormatDescription FormatDescription { get; private set; }

        public void Initialize(IGpi localGpi)
        {
            Debug.WriteLine(this, "Begin initializing");
            LocalGpi = localGpi;
            FormatDescription = VideoFormatDescription.Descriptions[VideoFormat];
            _frameTicks = FormatDescription.FrameTicks;
            _frameRate = FormatDescription.FrameRate;
            var chPGM = PlayoutChannelPGM;
            Debug.WriteLine(chPGM, "About to initialize");
            Debug.Assert(chPGM != null && chPGM.OwnerServer != null, "Null channel PGM or its server");
            var chPRV = PlayoutChannelPRV;
            if (chPRV != null
                && chPRV != chPGM
                && chPRV.OwnerServer != null)
            {
                ((CasparServer)chPRV.OwnerServer).MediaManager = this.MediaManager as MediaManager;
                chPRV.OwnerServer.Initialize();
                chPRV.OwnerServer.MediaDirectory.DirectoryName = chPRV.ChannelName;
            }

            if (chPGM != null
                && chPGM.OwnerServer != null)
            {
                ((CasparServer)chPGM.OwnerServer).MediaManager = this.MediaManager as MediaManager;
                chPGM.OwnerServer.Initialize();
                chPGM.OwnerServer.MediaDirectory.DirectoryName = chPGM.ChannelName;
            }

            MediaManager.Initialize();

            Debug.WriteLine(this, "Reading Root Events");
            this.DbReadRootEvents();
            this.DbReadTemplates();

            EngineState = TEngineState.Idle;
            var gpi = _gpi;
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
                            e(this, new EngineTickEventArgs(CurrentTime, _getTimeToAttention()));
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

            var gpi = _gpi;
            if (gpi != null)
            {
                Debug.WriteLine(this, "Uninitializing GPI");
                gpi.Started -= StartLoaded;
                gpi.UnInitialize();
                gpi.PropertyChanged -= GPI_PropertyChanged;
            }

            Debug.WriteLine(this, "Engine uninitialized");
        }

        #region GPI

        void GPI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("GPI"+e.PropertyName);
        }

        public bool GPIConnected
        {
            get { return _gpi != null && _gpi.Connected; }
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
            get { return _gpi != null && _gpi.AspectNarrow; }
            set { if (_gpi != null && _gPIEnabled) _gpi.AspectNarrow = value; }
        }

        [XmlIgnore]
        public TCrawl GPICrawl
        {
            get { return _gpi == null ? TCrawl.NoCrawl : (TCrawl)_gpi.Crawl; }
            set { if (_gpi != null && _gPIEnabled) _gpi.Crawl = (int)value; }
        }

        [XmlIgnore]
        public TLogo GPILogo
        {
            get { return _gpi == null ? TLogo.NoLogo : (TLogo)_gpi.Logo; }
            set { if (_gpi != null && _gPIEnabled) _gpi.Logo = (int)value; }
        }

        [XmlIgnore]
        public TParental GPIParental
        {
            get { return _gpi == null ? TParental.None : (TParental)_gpi.Parental; }
            set { if (_gpi != null && _gPIEnabled) _gpi.Parental = (int)value; }
        }

        [XmlIgnore]
        public bool GPIIsMaster
        {
            get { return _gpi != null && _gpi.IsMaster; }
        }
        #endregion // GPI

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

        
        readonly SynchronizedCollection<IEvent> _rootEvents = new SynchronizedCollection<IEvent>();

        [XmlIgnore]
        public SynchronizedCollection<IEvent> RootEvents { get { return _rootEvents; } }

        public IEvent CreateEvent()
        {
            return new Event(this);
        }

        public void AddEvent(IEvent aEvent)
        {
            aEvent.Saved += _eventSaved;
        }

        public enum ArchivePolicyType { NoArchive, ArchivePlayedAndNotUsedWhenDeleteEvent };

        public ArchivePolicyType ArchivePolicy;

        public void RemoveEvent(IEvent aEvent)
        {
            _rootEvents.Remove(aEvent);
            aEvent.Saved -= _eventSaved;
            ServerMedia media = (ServerMedia)aEvent.Media;
            if (aEvent.PlayState == TPlayState.Played 
                && media != null 
                && media.MediaType == TMediaType.Movie 
                && ArchivePolicy == Engine.ArchivePolicyType.ArchivePlayedAndNotUsedWhenDeleteEvent
                && MediaManager.ArchiveDirectory != null
                && CanDeleteMedia(media).Reason == MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                ThreadPool.QueueUserWorkItem(o => MediaManager.ArchiveMedia(new IMedia[] { media }, true));
        }

        private void _onServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var handler = ServerPropertyChanged;
            if (handler != null)
                handler(sender, e);
        }

        public IEvent PlayingEvent(VideoLayer layer = VideoLayer.Program)
        {
            return _visibleEvents[layer];
        }
             
        #region Preview Routines

        private IServerMedia _previewMedia;
        public  IServerMedia PreviewMedia { get { return _previewMedia; } }

        public void PreviewLoad(IServerMedia media, long seek, long duration, long position)
        {
            if (media != null)
            {
                _previewDuration = duration;
                _previewSeek = seek;
                _previewPosition = position;
                _previewMedia = media;
                PlayoutChannelPRV.SetAspect(VideoLayer.Preview, media.VideoFormat == TVideoFormat.NTSC
                                            || media.VideoFormat == TVideoFormat.PAL
                                            || media.VideoFormat == TVideoFormat.PAL_P);
                PlayoutChannelPRV.Load(media, VideoLayer.Preview, seek+position, duration-position);
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
                    if (newSeek != _previewPosition)
                    {
                        PlayoutChannelPRV.Load(_previewMedia, VideoLayer.Preview, _previewSeek + newSeek, _previewDuration - newSeek);
                        _previewPosition = newSeek;
                    }
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
                        foreach (Event ev in _runningEvents.Where(e => (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) && e.IsFinished).ToList())
                        {
                            _pause(ev, true);
                            Debug.WriteLine(ev, "Hold: Played");
                        }
                    if (value == TEngineState.Idle && _runningEvents.Count>0)
                    {
                        foreach (Event ev in _runningEvents.Where(e => (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) && e.IsFinished).ToList())
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

        public void ReScheduleAsync(IEvent aEvent)
        {
            ThreadPool.QueueUserWorkItem(o => ReSchedule(aEvent as Event));
        }

        public object RundownSync = new object();
        
        public void ReSchedule(Event aEvent)
        {
            lock (RundownSync)
            {
                if (aEvent == null)
                    return;
                try
                {
                    if (aEvent.PlayState == TPlayState.Aborted
                        || aEvent.PlayState == TPlayState.Played)
                    {
                        aEvent.PlayState = TPlayState.Scheduled;
                        foreach (Event se in aEvent.SubEvents.ToList())
                            ReSchedule(se);
                    }
                    else
                        aEvent.UpdateScheduledTime(false);
                    Event ne = aEvent.Next as Event;
                    if (ne == null)
                        ne = aEvent.GetSuccessor() as Event;
                    ReSchedule(ne);
                }
                finally
                {
                    aEvent.Save();
                }
            }
        }

        private bool _load(IEvent aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
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
            _run(aEvent);
            aEvent.PlayState = TPlayState.Paused;
            foreach (Event se in (aEvent.SubEvents.Where(e => e.ScheduledDelay == TimeSpan.Zero)).ToList())
                _load(se);
            return true;
        }

        private bool _loadNext(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetSuccessor() as Event;
            if (aEvent == null)
                return false;
            Debug.WriteLine(aEvent, "LoadNext");
            if (aEvent.PlayState == TPlayState.Scheduled || aEvent.PlayState == TPlayState.Played || aEvent.PlayState == TPlayState.Aborted)
                aEvent.PlayState = TPlayState.Scheduled;
            if (aEvent.EventType != TEventType.Rundown)
            {
                if (PlayoutChannelPGM != null)
                    PlayoutChannelPGM.LoadNext(aEvent);
                if (PlayoutChannelPRV != null)
                    PlayoutChannelPRV.LoadNext(aEvent);
                _loadedNextEvents[aEvent.Layer] = aEvent;
                if (_gpi != null
                    && GPIEnabled
                    && _gpi.GraphicsStartDelay < 0)
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        Thread.Sleep(_preloadTime + TimeSpan.FromMilliseconds(_gpi.GraphicsStartDelay));
                        _setGPIGraphics(_gpi, aEvent);
                    });
                }
            }
            _run(aEvent);
            foreach (Event e in aEvent.SubEvents.ToList())
                if (e.ScheduledDelay.Ticks  < _frameTicks)
                    _loadNext(e);
            return true;
        }

        private bool _play(Event aEvent, bool fromBeginning)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetSuccessor() as Event;
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
                if (LocalGpi != null && GPIEnabled)
                    _setGPIGraphics(LocalGpi, aEvent);
                if (_gpi != null && GPIEnabled)
                {
                    if (_gpi.GraphicsStartDelay <= 0)
                        _setGPIGraphics(_gpi, aEvent);
                    if (_gpi.GraphicsStartDelay > 0)
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            Thread.Sleep(_gpi.GraphicsStartDelay);
                            _setGPIGraphics(_gpi, aEvent);
                        });
                    }
                }
            }
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
                ThreadPool.QueueUserWorkItem(o => aEvent.AsRunLogWrite());
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

        private void _setAspectRatio(IEvent aEvent)
        {
            if (aEvent == null || !(aEvent.Layer == VideoLayer.Program || aEvent.Layer == VideoLayer.Preset))
                return;
            IMedia media = aEvent.Media;
            bool narrow = media != null && (media.VideoFormat == TVideoFormat.PAL || media.VideoFormat == TVideoFormat.NTSC || media.VideoFormat == TVideoFormat.PAL_P);
            if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
            {
                if (PlayoutChannelPGM != null)
                    PlayoutChannelPGM.SetAspect(VideoLayer.Program, narrow);
                if (PlayoutChannelPRV != null)
                    PlayoutChannelPRV.SetAspect(VideoLayer.Program, narrow);
            }
            if (AspectRatioControl == TAspectRatioControl.GPI || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                if (_gpi != null)
                    _gpi.AspectNarrow = narrow;
        }

        public void Load(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, "Load");
            lock (_tickLock)
            {
                EngineState = TEngineState.Hold;
                IEnumerable<IEvent> oldEvents = _visibleEvents.Values.ToList().Concat(_finishedEvents.Values.ToList());
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
                            IEvent s = e.GetSuccessor();
                            if (s != null)
                                s.UpdateScheduledTime(true);
                        }
                    }
                    EngineState = TEngineState.Running;
                }
        }

        public void Start(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, "Start");
            lock (_tickLock)
            {
                EngineState = TEngineState.Running;
                IEnumerable<IEvent> oldEvents = _visibleEvents.Values.ToList().Concat(_finishedEvents.Values.ToList());
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

                _play(aEvent as Event, true);
                foreach (Event e in oldEvents)
                    if (_visibleEvents[e.Layer] == null)
                        Clear(e.Layer);
            }
            NotifyEngineOperation(aEvent, TEngineOperation.Start);
        }

        public void Schedule(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, string.Format("Schedule {0}", aEvent.PlayState));
            lock (_tickLock)
                EngineState = TEngineState.Running;
            _run(aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Schedule);
        }

        private void _run(IEvent aEvent)
        {
            if (aEvent == null)
                return;
            lock (_tickLock)
            {
                if (!_runningEvents.Contains(aEvent))
                {
                    aEvent.PlayState = TPlayState.Scheduled;
                    _runningEvents.Add(aEvent);
                }
            }
        }

        private void _stop(IEvent aEvent)
        {
            aEvent.PlayState = TPlayState.Played;
            aEvent.Save();
            lock (_visibleEvents)
                if (_visibleEvents[aEvent.Layer] == aEvent)
                {
                    var le = _loadedNextEvents[aEvent.Layer];
                    if (aEvent.EventType != TEventType.Live
                        && (le == null || (le.ScheduledTime.Ticks - CurrentTicks >= _frameTicks)))
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

        private void _pause(IEvent aEvent, bool finish)
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
                    foreach (IEvent se in aEvent.SubEvents.ToList())
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
            IEvent ev;
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

        public void RestartRundown(IEvent ARundown)
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

            Event ev = ARundown as Event;
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
                    ev = ev.GetSuccessor() as Event;
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
                    foreach (IEvent e in _runningEvents)
                        if (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading)
                            e.Position += nFrames;

                    Event playingEvent = _visibleEvents[VideoLayer.Program] as Event;
                    if (playingEvent != null)
                    {
                        Event succEvent = playingEvent.IsLoop ? playingEvent : playingEvent.GetSuccessor() as Event;
                        if (succEvent == null)
                            succEvent = playingEvent.GetVisualRootTrack().FirstOrDefault(e => e.IsLoop) as Event;
                        if (succEvent != null)
                        {
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - succEvent.TransitionTime.Ticks)
                            {
                                if (playingEvent.PlayState == TPlayState.Playing)
                                {
                                    playingEvent.PlayState = TPlayState.Fading;
                                    Debug.WriteLine(playingEvent, "Tick: Fading");
                                }
                            }
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - _preloadTime.Ticks  
                                && !_runningEvents.Contains(succEvent))
                            {
                                // second: preload next scheduled events
                                Debug.WriteLine(succEvent, "Tick: LoadNext Running");
                                _loadNext(succEvent);
                            }
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - succEvent.TransitionTime.Ticks)
                            {
                                if (succEvent.PlayState == TPlayState.Scheduled)
                                {
                                    if (succEvent.IsHold)
                                        EngineState = TEngineState.Hold;
                                    else
                                    {
                                        Debug.WriteLine(succEvent, string.Format("Tick: Play current time: {0} scheduled time: {1}", CurrentTime, succEvent.ScheduledTime));
                                        _play(succEvent, true);
                                    }
                                }
                            }
                        }
                    }

                    IEnumerable<IEvent> runningEvents = null;
                    lock (_runningEvents.SyncRoot)
                        runningEvents = _runningEvents.Where(e => e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading).ToList();
                    foreach (IEvent e in runningEvents)
                        if (e.IsFinished)
                            _stop(e);
                    
                    lock (_runningEvents.SyncRoot)
                    {
                        if (!_runningEvents.Any(e => !e.IsFinished))
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

        private void _setGPIGraphics(IGpi gpi, Event ev)
        {
            if (ev.GPI.CanTrigger)
            {
                gpi.Crawl = (int)ev.GPI.Crawl;
                gpi.Logo = (int)ev.GPI.Logo;
                gpi.Parental = (int)ev.GPI.Parental;
            }
        }
        
        public MediaDeleteDenyReason CanDeleteMedia(IServerMedia serverMedia)
        {
            MediaDeleteDenyReason reason = MediaDeleteDenyReason.NoDeny;
            foreach (Event e in _rootEvents.ToList())
            {
                reason = e.CheckCanDeleteMedia(serverMedia);
                if (reason.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                    return reason;
            }
            return serverMedia.DbMediaInUse();
        }

        [XmlIgnore]
        public ICollection<IEvent> VisibleEvents { get { return _visibleEvents.Values; } }
        [XmlIgnore]
        public ICollection<IEvent> LoadedNextEvents { get { return _loadedNextEvents.Values; } }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        private void _onRunningSubEventsChanged(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (e.Operation == TCollectionOperation.Remove)
                _stop(e.Item);
            else
            {
                lock (_tickLock)
                {
                    TPlayState ps = ((Event)sender).PlayState;
                    if ((ps == TPlayState.Playing || ps == TPlayState.Paused)
                        && e.Item.PlayState == TPlayState.Scheduled)
                    {
                        e.Item.Position = ((Event)sender).Position;
                        _play(e.Item as Event, false);
                    }
                }
            }
        }

        private TimeSpan _getTimeToAttention()
        {
            IEvent pe = PlayingEvent();
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

        protected virtual void NotifyEngineOperation(IEvent aEvent, TEngineOperation operation)
        {
            var handler = EngineOperation; 
            if (handler != null)
                handler(this, new EngineOperationEventArgs(aEvent, operation));
        }

        public event EventHandler<DictionaryOperationEventArgs<VideoLayer, IEvent>> VisibleEventsOperation;
        private void _visibleEventsOperation(object o, DictionaryOperationEventArgs<VideoLayer, IEvent> e)
        {
            var handler = VisibleEventsOperation;
            if (handler != null)
                handler(o, e);
        }

        public event EventHandler<DictionaryOperationEventArgs<VideoLayer, IEvent>> LoadedNextEventsOperation;
        private void _loadedNextEventsOperation(object o, DictionaryOperationEventArgs<VideoLayer, IEvent> e)
        {
            var handler = LoadedNextEventsOperation;
            if (handler != null)
                handler(o, e);
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        private void _runningEventsOperation(object sender, CollectionOperationEventArgs<IEvent> e)
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

        public void SearchMissingEvents()
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

}