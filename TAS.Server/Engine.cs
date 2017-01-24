using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.Database;
using System.Collections.Concurrent;
using TAS.Remoting.Server;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace TAS.Server
{
    public class Engine : DtoBase, IEngine, IEnginePersistent, IDisposable
    {
        #region static methods        
        [DllImport("kernel32.dll")]
        private extern static int QueryUnbiasedInterruptTime(out ulong UnbiasedTime);

        #endregion // static methods
        #region IEnginePersistent
        public UInt64 Id { get; set; }
        public UInt64 Instance { get; set; }
        public UInt64 IdArchive { get; set; }
        public ulong IdServerPRI { get; set; }
        public int ServerChannelPRI { get; set; }
        public ulong IdServerSEC { get; set; }
        public int ServerChannelSEC { get; set; }
        public ulong IdServerPRV { get; set; }
        public int ServerChannelPRV { get; set; }
        public int CGStartDelay { get; set; }
        #endregion //IEnginePersistent

        string _engineName;
        [JsonProperty]
        public string EngineName
        {
            get { return _engineName; }
            set { SetField(ref _engineName, value, "EngineName"); }
        }
        #region Fields

        [JsonProperty(nameof(MediaManager))]
        private readonly IMediaManager _mediaManager;
        [XmlIgnore]
        public IMediaManager MediaManager { get { return _mediaManager; } }
        [XmlIgnore]

        Thread _engineThread;
        internal long CurrentTicks;

        public object RundownSync = new object();
        private static TimeSpan _preloadTime = new TimeSpan(0, 0, 2); // time to preload event
        readonly ObservableSynchronizedCollection<IEvent> _visibleEvents = new ObservableSynchronizedCollection<IEvent>(); // list of visible events
        readonly ObservableSynchronizedCollection<IEvent> _runningEvents = new ObservableSynchronizedCollection<IEvent>(); // list of events loaded and playing 
        readonly ConcurrentDictionary<VideoLayer, IEvent> _preloadedEvents = new ConcurrentDictionary<VideoLayer, IEvent>();
        readonly SynchronizedCollection<IEvent> _rootEvents = new SynchronizedCollection<IEvent>();
        readonly ConcurrentDictionary<ulong, IEvent> _events = new ConcurrentDictionary<ulong, IEvent>();

        private Event _forcedNext;

        public event EventHandler<EngineTickEventArgs> EngineTick;
        public event EventHandler<EngineOperationEventArgs> EngineOperation;
        [JsonProperty]
        public bool EnableCGElementsForNewEvents { get; set; }
        [JsonProperty]
        public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }
        private IEnumerable<IGpi> _localGpis;
        private IEnumerable<IEnginePlugin> _plugins;
        [JsonProperty(nameof(CGElementsController))]
        private ICGElementsController _cgElementsController;
        public ICGElementsController CGElementsController { get { return _cgElementsController; } }

        public RemoteClientHost Remote { get; set; }
        public TAspectRatioControl AspectRatioControl { get; set; }
        public double VolumeReferenceLoudness { get; set; }

        public int TimeCorrection { get { return (int)_timeCorrection.TotalMilliseconds; } set { _timeCorrection = TimeSpan.FromMilliseconds(value); } }
        protected TimeSpan _timeCorrection;

        #endregion Fields

        #region Constructor
        public Engine()
        {
            _visibleEvents.CollectionOperation += _visibleEventsOperation;
            _runningEvents.CollectionOperation += _runningEventsOperation;
            _engineState = TEngineState.NotInitialized;
            _mediaManager = new MediaManager(this);
            Database.Database.ConnectionStateChanged += _database_ConnectionStateChanged;
        }

        #endregion Constructor

        #region IDisposable implementation

        protected override void DoDispose()
        {
            base.DoDispose();
            _visibleEvents.CollectionOperation -= _visibleEventsOperation;
            _runningEvents.CollectionOperation -= _runningEventsOperation;
            foreach (Event e in _rootEvents)
                e.SaveLoadedTree();
            if (_cgElementsController != null)
                _cgElementsController.Dispose();
            var remote = Remote;
            if (remote != null)
                remote.Dispose();
            Database.Database.ConnectionStateChanged -= _database_ConnectionStateChanged;
        }

        #endregion //IDisposable

        static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(Engine));

        private CasparServerChannel _playoutChannelPRI;
        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPRI { get { return _playoutChannelPRI; } }

        private CasparServerChannel _playoutChannelSEC;
        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelSEC { get { return _playoutChannelSEC; } }

        private CasparServerChannel _playoutChannelPRV;

        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPRV { get { return _playoutChannelPRV; } }

        long _frameTicks;
        public long FrameTicks { get { return _frameTicks; } }
        RationalNumber _frameRate;

        [XmlIgnore]
        [JsonProperty]
        public RationalNumber FrameRate { get { return _frameRate; } }

        [JsonProperty]
        public TVideoFormat VideoFormat { get; set; }

        [XmlIgnore]
        [JsonProperty(IsReference = false)]
        public VideoFormatDescription FormatDescription { get; private set; }

        public void Initialize(IEnumerable<IPlayoutServer> servers)
        {
            Debug.WriteLine(this, "Begin initializing");
            Logger.Debug("Initializing engine {0}", this);

            var sPRI = servers.FirstOrDefault(S => S.Id == IdServerPRI);
            _playoutChannelPRI = sPRI == null ? null : (CasparServerChannel)sPRI.Channels.FirstOrDefault(c => c.ChannelNumber == ServerChannelPRI);
            var sSEC = servers.FirstOrDefault(S => S.Id == IdServerSEC);
            _playoutChannelSEC = sSEC == null ? null : (CasparServerChannel)sSEC.Channels.FirstOrDefault(c => c.ChannelNumber == ServerChannelSEC);
            var sPRV = servers.FirstOrDefault(S => S.Id == IdServerPRV);
            _playoutChannelPRV = sPRV == null ? null : (CasparServerChannel)sPRV.Channels.FirstOrDefault(c => c.ChannelNumber == ServerChannelPRV);

            _localGpis = this.ComposeParts<IGpi>();
            _plugins = this.ComposeParts<IEnginePlugin>();
            _cgElementsController = this.ComposePart<ICGElementsController>();

            FormatDescription = VideoFormatDescription.Descriptions[VideoFormat];
            _frameTicks = FormatDescription.FrameTicks;
            _frameRate = FormatDescription.FrameRate;
            var chPRI = PlayoutChannelPRI;
            var chSEC = PlayoutChannelSEC;
            if (chSEC != null
                && chSEC != chPRI)
            {
                ((CasparServer)chSEC.OwnerServer).MediaManager = this.MediaManager as MediaManager;
                chSEC.OwnerServer.Initialize();
                chSEC.OwnerServer.MediaDirectory.DirectoryName = chSEC.ChannelName;
                chSEC.OwnerServer.PropertyChanged += _server_PropertyChanged;
            }

            if (chPRI != null)
            {
                ((CasparServer)chPRI.OwnerServer).MediaManager = this.MediaManager as MediaManager;
                chPRI.OwnerServer.Initialize();
                chPRI.OwnerServer.MediaDirectory.DirectoryName = chPRI.ChannelName;
                chPRI.OwnerServer.PropertyChanged += _server_PropertyChanged;
            }

            MediaManager.Initialize();

            Debug.WriteLine(this, "Reading Root Events");
            this.DbReadRootEvents();

            EngineState = TEngineState.Idle;
            var cgElementsController = _cgElementsController;
            if (cgElementsController != null)
            {
                Debug.WriteLine(this, "Initializing CGElementsController");
                cgElementsController.Started += _startLoaded;
            }

            if (Remote != null)
            {
                Debug.WriteLine(this, "Initializing Remote interface");
                Remote.Initialize(this);
            }

            if (_localGpis != null)
                foreach (var gpi in _localGpis)
                    gpi.Started += _startLoaded;

            Debug.WriteLine(this, "Creating engine thread");
            _engineThread = new Thread(_engineThreadProc);
            _engineThread.Priority = ThreadPriority.Highest;
            _engineThread.Name = $"Engine main thread for {EngineName}";
            _engineThread.IsBackground = true;
            _engineThread.Start();
            Debug.WriteLine(this, "Engine initialized");
            Logger.Debug("Engine {0} initialized", this);
        }

        internal void UnInitialize()
        {
            Debug.WriteLine(this, "Aborting engine thread");
            _engineThread.Abort();
            _engineThread.Join();
            EngineState = TEngineState.NotInitialized;

            var chPRI = PlayoutChannelPRI;
            var chSEC = PlayoutChannelSEC;
            if (chSEC != null
                && chSEC != chPRI)
                chSEC.OwnerServer.PropertyChanged -= _server_PropertyChanged;
            if (chPRI != null)
                chPRI.OwnerServer.PropertyChanged -= _server_PropertyChanged;

            if (Remote != null)
            {
                Debug.WriteLine(this, "UnInitializing Remote interface");
                Remote.UnInitialize(this);
            }
            if (_localGpis != null)
                foreach (var gpi in _localGpis)
                    gpi.Started -= _startLoaded;

            var cgElementsController = _cgElementsController;
            if (cgElementsController != null)
            {
                Debug.WriteLine(this, "Uninitializing CGElementsController");
                cgElementsController.Started -= _startLoaded;
                cgElementsController.Dispose();
            }

            Debug.WriteLine(this, "Engine uninitialized");
        }

        private void _startLoaded(object o, EventArgs e)
        {
            StartLoaded();
        }

        private void _engineThreadProc()
        {
            Debug.WriteLine(this, "Engine thread started");
            Logger.Debug("Started engine thread for {0}", this);
            CurrentTime = AlignDateTime(DateTime.UtcNow + _timeCorrection);
            CurrentTicks = CurrentTime.Ticks;

            List<IEvent> playingEvents = this.DbSearchPlaying();
            IEvent playing = playingEvents.FirstOrDefault(e => e.Layer == VideoLayer.Program && (e.EventType == TEventType.Live || e.EventType == TEventType.Movie));
            if (playing != null)
            {
                Debug.WriteLine(playing, "Playing event found");
                if (CurrentTicks < (playing.ScheduledTime + playing.Duration).Ticks)
                {
                    foreach (var e in playingEvents)
                    {
                        ((Event)e).Position = (CurrentTicks - e.ScheduledTime.Ticks) / _frameTicks;
                        _runningEvents.Add(e);
                        _visibleEvents.Add(e);
                    }
                    _engineState = TEngineState.Running;
                    Playing = playing;
                }
                else
                    foreach (var e in playingEvents)
                    {
                        e.PlayState = TPlayState.Aborted;
                        e.Save();
                    }
            }
            else
                foreach (var e in playingEvents)
                {
                    e.PlayState = TPlayState.Aborted;
                    e.Save();
                }

            ulong currentUnbiasedTime;
            ulong previousUnbiasedTime;
            QueryUnbiasedInterruptTime(out currentUnbiasedTime);
            previousUnbiasedTime = currentUnbiasedTime;
            ulong frameUnbiasedTime = (ulong)_frameTicks;
            TimeSpan frameDuration = TimeSpan.FromTicks(_frameTicks);
            while (!IsDisposed)
            {
                try
                {
                    CurrentTime = AlignDateTime(DateTime.UtcNow + _timeCorrection);
                    QueryUnbiasedInterruptTime(out currentUnbiasedTime);
                    CurrentTicks = CurrentTime.Ticks;
                    ulong nFrames = (currentUnbiasedTime - previousUnbiasedTime) / frameUnbiasedTime;
                    previousUnbiasedTime = currentUnbiasedTime;
                    _tick((long)nFrames);
                    EngineTick?.Invoke(this, new EngineTickEventArgs(CurrentTime, _getTimeToAttention()));
                    if (nFrames > 1)
                    {
                        Debug.WriteLine(nFrames, "LateFrame");
                        if (nFrames > 20)
                            Logger.Error("LateFrame: {0}", nFrames);
                        else
                            Logger.Warn("LateFrame: {0}", nFrames);
                    }
#if DEBUG
                    Debug.WriteLineIf(nFrames == 0, "Zero frames tick");
#endif
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e, "Exception in engine tick");
                    Logger.Error($"{e}");
                }
                QueryUnbiasedInterruptTime(out currentUnbiasedTime);
                int waitTime = (int)((frameUnbiasedTime - currentUnbiasedTime + previousUnbiasedTime + 10000) / 10000);
                if (waitTime > 0)
                    Thread.Sleep(waitTime);
#if DEBUG
                else
                    Debug.WriteLine("Negative waitTime");
#endif
            }
            Debug.WriteLine(this, "Engine thread finished");
            Logger.Debug("Engine thread finished: {0}", this);
        }

        private void _server_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<CasparServerChannel, List<IEvent>> channelConnected = (channel, ve) =>
            {
                foreach (Event ev in ve)
                {
                    channel.ReStart(ev);
                    channel.SetVolume(VideoLayer.Program, _programAudioVolume, 0);
                    if (ev.Layer == VideoLayer.Program || ev.Layer == VideoLayer.Preset)
                    {
                        IMedia media = ev.Media;
                        bool narrow = media != null && (media.VideoFormat == TVideoFormat.PAL || media.VideoFormat == TVideoFormat.NTSC || media.VideoFormat == TVideoFormat.PAL_P);
                        if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                            channel.SetAspect(VideoLayer.Program, narrow);
                    }
                }
            };

            if (e.PropertyName == nameof(IPlayoutServer.IsConnected) && ((IPlayoutServer)sender).IsConnected)
            {
                var ve = _visibleEvents.ToList();
                if (PlayoutChannelPRI != null
                    && sender == PlayoutChannelPRI.OwnerServer)
                    channelConnected(_playoutChannelPRI, ve);
                if (PlayoutChannelSEC != null
                    && sender == PlayoutChannelSEC.OwnerServer
                    && PlayoutChannelSEC != PlayoutChannelPRI)
                    channelConnected(_playoutChannelSEC, ve);
            }
        }

        #region Database
        private void _database_ConnectionStateChanged(object sender, RedundantConnectionStateEventArgs e)
        {
            NotifyPropertyChanged(nameof(DatabaseConnectionState));
            Logger.Trace("Database state changed from {0} to {1}. Stack trace was {2}", e.OldState, e.NewState, new StackTrace());
        }

        public ConnectionStateRedundant DatabaseConnectionState
        {
            get { return Database.Database.ConnectionState; }
        }

        #endregion //Database
                
        #region FixedStartEvents

        readonly SynchronizedCollection<Event> _fixedTimeEvents = new SynchronizedCollection<Event>();
        internal void AddFixedTimeEvent(Event e)
        {
            _fixedTimeEvents.Add(e);
            FixedTimeEventOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, TCollectionOperation.Insert));
        }
        internal void RemoveFixedTimeEvent(Event e)
        {
            if (_fixedTimeEvents.Remove(e))
            {
                FixedTimeEventOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, TCollectionOperation.Remove));
            }
        }

        [XmlIgnore]
        public List<IEvent> FixedTimeEvents { get { lock(_fixedTimeEvents.SyncRoot) return _fixedTimeEvents.Cast<IEvent>().ToList(); } }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;

        #endregion // FixedStartEvents

        public TArchivePolicyType ArchivePolicy;
                
        private Event _playing;
        [XmlIgnore]
        public IEvent Playing
        {
            get { return _playing; }
            private set
            {
                var oldPlaying = _playing;
                if (SetField(ref _playing, (Event)value, nameof(Playing)))
                {
                    if (oldPlaying != null)
                        oldPlaying.SubEventChanged -= _playingSubEventsChanged;
                    if (value != null)
                    {
                        value.SubEventChanged += _playingSubEventsChanged;
                        var media = value.Media;
                        SetField(ref _fieldOrderInverted, media == null ? false : media.FieldOrderInverted, nameof(FieldOrderInverted));
                    }
                }
            }
        }

        public IEvent NextToPlay
        {
            get
            {
                var e = _playing;
                if (e != null)
                {
                    e = _successor(e);
                    if (e != null)
                    {
                        if (e.EventType == TEventType.Rundown)
                            return e.FindVisibleSubEvent();
                        else
                            return e;
                    }
                }
                return null;
            }
        }

        public IEvent NextWithRequestedStartTime
        {
            get
            {
                Event e = _playing;
                if (e != null)
                    do
                        e = e.getSuccessor();
                    while (e != null && e.RequestedStartTime == null);
                return e;
            }
        }
    
        #region Preview Routines

        private IMedia _previewMedia;
        [JsonProperty]
        public  IMedia PreviewMedia { get { return _previewMedia; } }

        public void PreviewLoad(IMedia media, long seek, long duration, long position, decimal previewAudioVolume)
        {
            Media mediaToLoad = _findPreviewMedia(media as Media);
            if (mediaToLoad != null)
            {
                _previewDuration = duration;
                _previewSeek = seek;
                _previewPosition = position;
                _previewMedia = media;
                _playoutChannelPRV.SetAspect(VideoLayer.Preview, media.VideoFormat == TVideoFormat.NTSC
                                            || media.VideoFormat == TVideoFormat.PAL
                                            || media.VideoFormat == TVideoFormat.PAL_P);
                PreviewLoaded = true;
                PreviewAudioLevel = previewAudioVolume;
                _playoutChannelPRV.Load(mediaToLoad, VideoLayer.Preview, seek+position, duration-position);
                PreviewIsPlaying = false;
                NotifyPropertyChanged(nameof(PreviewMedia));
                NotifyPropertyChanged(nameof(PreviewPosition));
                NotifyPropertyChanged(nameof(PreviewSeek));
            }
        }

        public void PreviewUnload()
        {
            var channel = _playoutChannelPRV;
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
                    NotifyPropertyChanged(nameof(PreviewMedia));
                    NotifyPropertyChanged(nameof(PreviewPosition));
                    NotifyPropertyChanged(nameof(PreviewSeek));
                }
            }
        }

        private long _previewDuration;

        private long _previewPosition;

        private long _previewSeek;
        [XmlIgnore]
        [JsonProperty]
        public long PreviewSeek { get { return _previewSeek; } }

        [XmlIgnore]
        [JsonProperty]
        public long PreviewPosition // from 0 to duration
        {
            get { return _previewPosition; }
            set
            {
                if (_playoutChannelPRV != null && _previewMedia!=null)
                {
                    PreviewPause();
                    long newSeek = value < 0 ? 0 : value;
                    long maxSeek = _previewDuration-1;
                    if (newSeek > maxSeek)
                        newSeek = maxSeek;
                    if (SetField(ref _previewPosition, newSeek, nameof(PreviewPosition)))
                    {
                        _playoutChannelPRV.Seek(VideoLayer.Preview, _previewSeek + newSeek);
                        _previewPosition = newSeek;
                    }
                }
            }
        }

        private decimal _previewAudioLevel;
        [XmlIgnore]
        public decimal PreviewAudioLevel
        {
            get { return _previewAudioLevel; }
            set
            {
                if (SetField(ref _previewAudioLevel, value, nameof(PreviewAudioLevel)))
                    _playoutChannelPRV.SetVolume(VideoLayer.Preview, (decimal)Math.Pow(10, (double)value / 20), 0);
            }
        }

        public void PreviewPlay()
        {
            var channel = _playoutChannelPRV;
            var media = PreviewMedia;
            if (channel != null && channel.Play(VideoLayer.Preview) && media != null)
                PreviewIsPlaying = true;
        }

        public void PreviewPause()
        {
            var channel = _playoutChannelPRV;
            if (PreviewIsPlaying 
                && channel != null 
                && channel.Pause(VideoLayer.Preview))
                PreviewIsPlaying = false;
        }

        private bool _previewLoaded;

        [XmlIgnore]
        [JsonProperty]
        public bool PreviewLoaded {
            get { return _previewLoaded; }
            private set
            {
                if (SetField(ref _previewLoaded, value, nameof(PreviewLoaded)))
                {
                    decimal vol = (_previewLoaded) ? 0 : _programAudioVolume;
                    if (_playoutChannelPRV != null)
                        _playoutChannelPRV.SetVolume(VideoLayer.Program, vol, 0);
                }
            }
        }

        private bool _previewIsPlaying;
        [XmlIgnore]
        [JsonProperty]
        public bool PreviewIsPlaying { get { return _previewIsPlaying; } private set { SetField(ref _previewIsPlaying, value, nameof(PreviewIsPlaying)); } }

        private Media _findPreviewMedia(Media media)
        {
            IPlayoutServerChannel playoutChannel = _playoutChannelPRV;
            if (media is ServerMedia)
            {
                if (playoutChannel == null)
                    return null;
                else
                    return media.Directory == playoutChannel.OwnerServer.MediaDirectory ? media : ((ServerDirectory)playoutChannel.OwnerServer.MediaDirectory).FindMediaByMediaGuid(media.MediaGuid);
            }
            else
                return media;
        }

        #endregion // Preview Routines

        #region private methods
        private bool _load(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.getSuccessor();
            if (aEvent == null)
                return false;
            Debug.WriteLine("{0} Load: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
            Logger.Info("{0} {1}: Load {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this, aEvent);
            var eventType = aEvent.EventType;
            if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage && aEvent is Event)
            {
                if (PlayoutChannelPRI != null)
                    _playoutChannelPRI.Load((Event)aEvent);
                if (PlayoutChannelSEC != null)
                    _playoutChannelSEC.Load((Event)aEvent);
                _visibleEvents.Add(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                    Playing = aEvent;
            }
            _run(aEvent);
            aEvent.PlayState = TPlayState.Paused;
            NotifyEngineOperation(aEvent, TEngineOperation.Load);
            foreach (Event se in (aEvent.SubEvents.Where(e => e.ScheduledDelay == TimeSpan.Zero)))
                _load(se);
            return true;
        }

        private bool _loadNext(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.getSuccessor();
            if (aEvent == null)
                return false;
            var eventType = aEvent.EventType;
            IEvent preloaded;
            if ((eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage) && 
                !(_preloadedEvents.TryGetValue(aEvent.Layer, out preloaded) && preloaded == aEvent))
            {
                Debug.WriteLine("{0} LoadNext: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
                Logger.Info("{0} {1}: Preload {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this, aEvent);
                _preloadedEvents[aEvent.Layer] = aEvent;
                if (_playoutChannelPRI != null)
                    _playoutChannelPRI.LoadNext((Event)aEvent);
                if (_playoutChannelSEC != null)
                    _playoutChannelSEC.LoadNext((Event)aEvent);
                var cgElementsController = _cgElementsController;
                if (!aEvent.IsHold
                    && cgElementsController?.IsConnected == true
                    && cgElementsController.IsCGEnabled
                    && CGStartDelay < 0)
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        Thread.Sleep(_preloadTime + TimeSpan.FromMilliseconds(CGStartDelay));
                        cgElementsController.SetState(aEvent);
                    });
                }
            }
            if (aEvent.SubEventsCount > 0)
                foreach (Event se in aEvent.SubEvents)
                {
                    se.PlayState = TPlayState.Scheduled;
                    var seType = se.EventType;
                    var seStartType = se.StartType;
                    if (seType == TEventType.Rundown 
                        || seType == TEventType.Live
                        || seType == TEventType.Movie
                        || (seType == TEventType.StillImage && (seStartType == TStartType.WithParent && se.ScheduledDelay < _preloadTime)
                                                             || (seStartType == TStartType.WithParentFromEnd && aEvent.Duration - se.Duration - se.ScheduledDelay < _preloadTime)))
                        _loadNext(se);
                }
            _run(aEvent);
            return true;
        }

        private bool _play(Event aEvent, bool fromBeginning)
        {
            var eventType = aEvent.EventType;
            if (aEvent != null && (!aEvent.IsEnabled || (aEvent.Length == TimeSpan.Zero && eventType != TEventType.Animation && eventType != TEventType.CommandScript)))
                aEvent = aEvent.getSuccessor();
            if (aEvent as Event == null)
                return false;
            Debug.WriteLine("{0} Play: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
            Logger.Info("{0} {1}: Play {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this, aEvent);
            eventType = aEvent.EventType;
            if (aEvent == _forcedNext)
            {
                ForcedNext = null;
                _runningEvents.ToList().ForEach(
                    e =>
                    {
                        if (e.PlayState == TPlayState.Playing)
                        {
                            e.PlayState = ((Event)e).IsFinished() ? TPlayState.Played : TPlayState.Aborted;
                            _runningEvents.Remove(e);
                        }
                        e.SaveDelayed();
                    });                        
            }
            _run(aEvent);
            if (fromBeginning)
                ((Event)aEvent).Position = 0;
            if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
            {
                if (_playoutChannelPRI != null)
                    _playoutChannelPRI.Play((Event)aEvent);
                if (_playoutChannelSEC != null)
                    _playoutChannelSEC.Play((Event)aEvent);
                _visibleEvents.Add(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                {
                    Playing = aEvent;
                    ProgramAudioVolume = (decimal)Math.Pow(10, (double)aEvent.GetAudioVolume() / 20); ;
                    _setAspectRatio(aEvent);
                    var cgController = _cgElementsController;
                    if (cgController?.IsConnected == true && cgController.IsCGEnabled)
                    {
                        if (CGStartDelay <= 0)
                            cgController.SetState(aEvent);
                        else
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                Thread.Sleep(CGStartDelay);
                                cgController.SetState(aEvent);
                            });
                        }
                    }
                }
                IEvent removed;
                _preloadedEvents.TryRemove(aEvent.Layer, out removed);
            }
            if (eventType == TEventType.Animation || eventType == TEventType.CommandScript)
            {
                if (_playoutChannelPRI != null)
                    _playoutChannelPRI.Play((Event)aEvent);
                if (_playoutChannelSEC != null)
                    _playoutChannelSEC.Play((Event)aEvent);
                aEvent.PlayState = TPlayState.Played;
            }
            else
            {
                aEvent.PlayState = TPlayState.Playing;
                if (aEvent.SubEventsCount > 0)
                    foreach (Event se in aEvent.SubEvents)
                        if (se.ScheduledDelay == TimeSpan.Zero)
                            _play(se, fromBeginning);
            }
            aEvent.SaveDelayed();
            if (_pst2Prv)
                _loadPST();
            NotifyEngineOperation(aEvent, TEngineOperation.Play);
            if (aEvent.Layer == VideoLayer.Program
                && (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.Live))
                ThreadPool.QueueUserWorkItem(o => ((Event)aEvent).AsRunLogWrite());
            return true;
        }

        private void _clearRunning()
        {
            Debug.WriteLine("_clearRunning");
            foreach (var e in _runningEvents.ToList())
            {
                if (e.PlayState == TPlayState.Playing)
                    e.PlayState = TPlayState.Aborted;
                if (e.PlayState == TPlayState.Fading)
                    e.PlayState = TPlayState.Played;
                if (e.PlayState == TPlayState.Paused)
                    e.PlayState = TPlayState.Scheduled;
                if (e.IsModified)
                    e.SaveDelayed();
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
                _playoutChannelPRI?.SetAspect(aEvent.Layer, narrow);
                _playoutChannelSEC?.SetAspect(aEvent.Layer, narrow);
            }
            if (AspectRatioControl == TAspectRatioControl.GPI || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
            {
                var cgController = _cgElementsController;
                if (cgController?.IsConnected == true && cgController.IsCGEnabled)
                    cgController.IsWideScreen = !narrow;
                var lGpis = _localGpis;
                if (lGpis != null)
                    foreach (var gpi in lGpis)
                        gpi.IsWideScreen = !narrow;
            }
        }

        private void _run(IEvent aEvent)
        {
            var eventType = aEvent.EventType;
            if (eventType == TEventType.Animation || eventType == TEventType.CommandScript)
                return;
            lock (_runningEvents.SyncRoot)
            {
                if (!_runningEvents.Contains(aEvent))
                    _runningEvents.Add(aEvent);
            }
        }

        private void _stop(IEvent aEvent)
        {
            aEvent.PlayState = ((Event)aEvent).Position == 0 ? TPlayState.Scheduled : ((Event)aEvent).IsFinished() ? TPlayState.Played : TPlayState.Aborted;
            aEvent.SaveDelayed();
            lock (_visibleEvents.SyncRoot)
                if (_visibleEvents.Contains(aEvent))
                {
                    var eventType = aEvent.EventType;
                    if (eventType != TEventType.Live && eventType != TEventType.CommandScript && aEvent is Event)
                    {
                        Debug.WriteLine("{0} Stop: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
                        Logger.Info("{0} {1}: Stop {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this, aEvent);
                        _playoutChannelPRI?.Stop((Event)aEvent);
                        _playoutChannelSEC?.Stop((Event)aEvent);
                    }
                    _visibleEvents.Remove(aEvent);
                }
            _runningEvents.Remove(aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Stop);
        }

        private void _pause(IEvent aEvent, bool finish)
        {
            lock (_visibleEvents)
                if (_visibleEvents.Contains(aEvent))
                {
                    Debug.WriteLine("{0} Pause: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
                    Logger.Info("{0} {1}: Pause {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this, aEvent);
                    if (aEvent.EventType != TEventType.Live && aEvent.EventType != TEventType.StillImage && aEvent is Event)
                    {
                        _playoutChannelPRI?.Pause((Event)aEvent);
                        _playoutChannelSEC?.Pause((Event)aEvent);
                    }
                    foreach (var se in aEvent.SubEvents)
                        _pause(se, finish);
                }
            if (finish)
            {
                aEvent.PlayState = TPlayState.Played;
                aEvent.SaveDelayed();
                _runningEvents.Remove(aEvent);
                NotifyEngineOperation(aEvent, TEngineOperation.Stop);
            }
            else
                NotifyEngineOperation(aEvent, TEngineOperation.Pause);
        }

        private void _loadPST()
        {
            Event ev = NextToPlay as Event;
            if (ev != null && PlayoutChannelPRV != null)
            {
                Media media = ev.ServerMediaPRV;
                if (media != null)
                {
                    _playoutChannelPRV.Load(media, VideoLayer.Preset, 0, -1);
                    return;
                }
            }
            _playoutChannelPRV.Load(System.Drawing.Color.Black, VideoLayer.Preset);
        }

        private void _restartEvent(IEvent ev)
        {
            Event e = ev as Event;
            if (e != null)
            {
                _playoutChannelPRI?.ReStart(e);
                _playoutChannelSEC?.ReStart(e);
            }
        }

        private object _tickLock = new object();
        private void _tick(long nFrames)
        {
            lock (_tickLock)
            {
                if (EngineState == TEngineState.Running)
                {
                    lock (_runningEvents.SyncRoot)
                        foreach (var e in _runningEvents.Where(ev => ev.PlayState == TPlayState.Playing || ev.PlayState == TPlayState.Fading))
                            ((Event)e).Position += nFrames;

                    Event playingEvent = _playing;
                    Event succEvent = null;
                    if (playingEvent != null)
                    {
                        succEvent = _successor(playingEvent);
                        if (succEvent != null)
                        {
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - succEvent.TransitionTime.Ticks)
                            {
                                if (playingEvent.PlayState == TPlayState.Playing)
                                    playingEvent.PlayState = TPlayState.Fading;
                            }
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - _preloadTime.Ticks)
                                _loadNext(succEvent);
                            if (playingEvent.Position >= playingEvent.LengthInFrames() - succEvent.TransitionInFrames())
                            {
                                if (succEvent.IsHold && succEvent != _forcedNext)
                                    EngineState = TEngineState.Hold;
                                else
                                    _play(succEvent, true);
                            }
                        }
                        playingEvent = _playing; // in case when succEvent just started 
                        if (playingEvent != null && playingEvent.SubEventsCount > 0)
                        {
                            TimeSpan playingEventPosition = TimeSpan.FromTicks(playingEvent.Position * _frameTicks);
                            TimeSpan playingEventDuration = playingEvent.Duration;
                            var sel = playingEvent.SubEvents.Where(e => e.PlayState == TPlayState.Scheduled);
                            foreach (Event se in sel)
                            {
                                IEvent preloaded;
                                TEventType eventType = se.EventType;
                                switch (se.StartType)
                                {
                                    case TStartType.WithParent:
                                        if ((eventType == TEventType.Movie || eventType == TEventType.StillImage)
                                            && playingEventPosition >= se.ScheduledDelay - _preloadTime - se.TransitionTime
                                            && !(_preloadedEvents.TryGetValue(se.Layer, out preloaded) && se == preloaded))
                                            _loadNext(se);
                                        if (playingEventPosition >= se.ScheduledDelay - se.TransitionTime)
                                            _play(se, true);
                                        break;
                                    case TStartType.WithParentFromEnd:
                                        if ((eventType == TEventType.Movie || eventType == TEventType.StillImage)
                                            && playingEventPosition >= playingEventDuration - se.Duration - se.ScheduledDelay - _preloadTime - se.TransitionTime
                                            && !(_preloadedEvents.TryGetValue(se.Layer, out preloaded) && se == preloaded))
                                            _loadNext(se);
                                        if (playingEventPosition >= playingEventDuration - se.Duration - se.ScheduledDelay - se.TransitionTime)
                                            _play(se, true);
                                        break;
                                }
                            }
                        }
                    }

                    IEnumerable<IEvent> runningEvents;
                    lock (_runningEvents.SyncRoot)
                        runningEvents = _runningEvents.Where(e => e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading).ToList();
                    foreach (Event e in runningEvents)
                    {
                        if (e.IsFinished())
                        {
                            if (succEvent == null)
                                _pause(e, true);
                            else
                                _stop(e);
                        }
                    }
                    if (_runningEvents.Count == 0)
                        EngineState = TEngineState.Idle;
                }

                _executeAutoStartEvents();

                // preview controls
                if (PreviewIsPlaying)
                {
                    if (_previewPosition < _previewDuration - 1)
                    {
                        _previewPosition += nFrames;
                        NotifyPropertyChanged(nameof(PreviewPosition));
                    }
                    else
                        PreviewPause();
                }
            }
        }


        private void _executeAutoStartEvents()
        {
            var currentTimeOfDayTicks = CurrentTime.TimeOfDay.Ticks;
            lock (_fixedTimeEvents.SyncRoot)
            {
                var startEvent = _fixedTimeEvents.FirstOrDefault(e =>
                                                                  e.StartType == TStartType.OnFixedTime
                                                               && (EngineState == TEngineState.Idle || (e.AutoStartFlags & AutoStartFlags.Force) == AutoStartFlags.Force)
                                                               && (e.PlayState == TPlayState.Scheduled || (e.PlayState != TPlayState.Playing && (e.AutoStartFlags & AutoStartFlags.Force) == AutoStartFlags.Force))
                                                               && e.IsEnabled
                                                               && ((e.AutoStartFlags & AutoStartFlags.Daily) == AutoStartFlags.Daily ?
                                                                    currentTimeOfDayTicks >= e.ScheduledTime.TimeOfDay.Ticks && currentTimeOfDayTicks < e.ScheduledTime.TimeOfDay.Ticks + TimeSpan.TicksPerSecond :
                                                                    CurrentTicks >= e.ScheduledTime.Ticks && CurrentTicks < e.ScheduledTime.Ticks + TimeSpan.TicksPerSecond) // auto start only within 1 second slot
                    ) as Event;
                if (startEvent != null)
                {
                    _runningEvents.Remove(startEvent);
                    startEvent.PlayState = TPlayState.Scheduled;
                    Start(startEvent);
                }
            }
        }

        private Event _successor(Event playingEvent)
        {
            Event result = _forcedNext as Event;
            if (result == null)
            {
                if (playingEvent != null)
                {
                    result = playingEvent.IsLoop ? playingEvent : playingEvent.getSuccessor();
                    if (result == null)
                        result = playingEvent.GetVisualRootTrack().FirstOrDefault(e => e.IsLoop) as Event;
                }
            }
            while (result != null && (!result.IsEnabled || (result.Length == TimeSpan.Zero)))
                   result = result.getSuccessor();
            return result;
        }

        private void _playingSubEventsChanged(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (_playing != sender)
                return;
            if (e.Operation == TCollectionOperation.Remove)
                _stop((Event)e.Item);
            else
            {
                lock (_tickLock)
                {
                    TPlayState ps = ((Event)sender).PlayState;
                    if ((ps == TPlayState.Playing || ps == TPlayState.Paused)
                        && e.Item.PlayState == TPlayState.Scheduled)
                    {
                        ((Event)e.Item).Position = ((Event)sender).Position;
                        if (ps == TPlayState.Paused)
                        {
                            if (e.Item.EventType == TEventType.StillImage)
                                _load(e.Item as Event);
                        }
                        else
                            _play(e.Item as Event, false);
                    }
                }
            }
        }

        private TimeSpan _getTimeToAttention()
        {
            Event pe = _playing;
            if (pe != null && (pe.PlayState == TPlayState.Playing || pe.PlayState == TPlayState.Paused))
            {
                TimeSpan result = pe.Length - TimeSpan.FromTicks(pe.Position * _frameTicks);
                pe = pe.getSuccessor();
                while (pe != null)
                {
                    TimeSpan? pauseTime = pe.GetAttentionTime();
                    if (pauseTime != null)
                        return result + pauseTime.Value - pe.TransitionTime;
                    result = result + pe.Length - pe.TransitionTime;
                    pe = pe.getSuccessor();
                }
                return result;
            }
            return TimeSpan.Zero;
        }

        [XmlIgnore]
        public DateTime CurrentTime { get; private set; }

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

        #endregion // private methods

        #region IEngine methods

        public void Load(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, "Load");
            lock (_tickLock)
            {
                EngineState = TEngineState.Hold;
                foreach (Event e in _visibleEvents.ToList())
                    _stop(e);
                foreach (Event e in _runningEvents.ToList())
                {
                    _runningEvents.Remove(e);
                    if (e.Position == 0)
                        e.PlayState = TPlayState.Scheduled;
                    else
                        e.PlayState = TPlayState.Aborted;
                }
            }
            _load(aEvent as Event);
        }

        public void StartLoaded()
        {
            Debug.WriteLine("StartLoaded executed");
            lock (_tickLock)
                if (EngineState == TEngineState.Hold)
                {
                    _visibleEvents.Where(e => e.PlayState == TPlayState.Played).ToList().ForEach(e => _stop(e));
                    foreach (Event e in _runningEvents.ToList())
                    {
                        if (!(e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading))
                            _play(e, false);
                    }
                    EngineState = TEngineState.Running;
                }
        }

        public void Start(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, "Start");
            var ets = aEvent as Event;
            if (ets == null)
                return;
            lock (_tickLock)
            {
                EngineState = TEngineState.Running;
                var eventsToStop = _visibleEvents.Where(e=> e.PlayState == TPlayState.Played || e.PlayState == TPlayState.Playing).ToList();
                foreach (Event e in _runningEvents.ToList())
                {
                    _runningEvents.Remove(e);
                    if (e.Position == 0)
                        e.PlayState = TPlayState.Scheduled;
                    else
                        e.PlayState = TPlayState.Aborted;
                }
                _play(ets, true);
                foreach (Event e in eventsToStop)
                    _stop(e);
            }
        }

        public void Schedule(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, $"Schedule {aEvent.PlayState}");
            lock (_tickLock)
                EngineState = TEngineState.Running;
            _run((Event)aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Schedule);
        }

        [XmlIgnore]
        public IEvent ForcedNext
        {
            get { return _forcedNext; }
            set
            {

                lock (_tickLock)
                {
                    var oldForcedNext = _forcedNext;
                    if (SetField(ref _forcedNext, value as Event, nameof(ForcedNext)))
                    {
                        Debug.WriteLine(value, "ForcedNext");
                        NotifyPropertyChanged(nameof(NextToPlay));
                        if (_forcedNext != null)
                            _forcedNext.IsForcedNext = true;
                        if (oldForcedNext != null)
                            oldForcedNext.IsForcedNext = false;
                    }
                }
            }
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            Debug.WriteLine(aVideoLayer, "Clear");
            Logger.Info("{0} {1}: Clear layer {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this, aVideoLayer);
            IEvent ev;
            lock (_visibleEvents.SyncRoot)
                ev = _visibleEvents.FirstOrDefault(e => e.Layer == aVideoLayer);
            if (ev != null)
            {
                ev.PlayState = TPlayState.Aborted;
                ev.SaveDelayed();
                _visibleEvents.Remove(ev);
                _runningEvents.Remove(ev);
            }
            if (ev != null)
            {
                ev.PlayState = TPlayState.Scheduled;
                ev.SaveDelayed();
                _runningEvents.Remove(ev);
            }
            if (_playoutChannelPRI != null)
                _playoutChannelPRI.Clear(aVideoLayer);
            if (_playoutChannelSEC != null)
                _playoutChannelSEC.Clear(aVideoLayer);
            if (aVideoLayer == VideoLayer.Program)
                lock(_tickLock)
                    Playing = null;
        }
        
        public void Clear()
        {
            Logger.Info("{0} {1}: Clear all", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this);
            _clearRunning();
            _visibleEvents.Clear();
            ForcedNext = null;
            if (_playoutChannelPRI != null)
                _playoutChannelPRI.Clear();
            if (_playoutChannelSEC != null)
                _playoutChannelSEC.Clear();
            PreviewUnload();
            NotifyEngineOperation(null, TEngineOperation.Clear);
            ProgramAudioVolume = 1.0m;
            lock (_tickLock)
            {
                EngineState = TEngineState.Idle;
                Playing = null;
            }
        }

        public void ClearMixer()
        {
            if (_playoutChannelPRI != null)
                _playoutChannelPRI.ClearMixer();
            if (_playoutChannelSEC != null)
                _playoutChannelSEC.ClearMixer();
        }

        public void Restart()
        {
            Logger.Info("{0} {1}: Restart", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), this);
            foreach (var e in _visibleEvents.ToList())
                _restartEvent(e);
        }

        public void RestartRundown(IEvent ARundown)
        {
            Action<Event> _rerun = (aEvent) =>
                {
                    if (!_runningEvents.Contains(aEvent))
                        _runningEvents.Add(aEvent);
                    if (aEvent.EventType != TEventType.Rundown)
                    {
                        _visibleEvents.Add(aEvent);
                        _restartEvent(aEvent);
                    }
                };

            Event ev = ARundown as Event;
            while (ev != null)
            {
                if (CurrentTicks >= ev.ScheduledTime.Ticks && CurrentTicks < ev.ScheduledTime.Ticks + ev.Duration.Ticks)
                {
                    ev.Position = (CurrentTicks - ev.ScheduledTime.Ticks) / _frameTicks;
                    var st = ev.StartTime;
                    ev.PlayState = TPlayState.Playing;
                    if (st != ev.StartTime)
                        ev.StartTime = st;
                    _rerun(ev);
                    foreach (var se in ev.SubEvents)
                        RestartRundown(se);
                    break;
                }
                else
                    ev = ev.getSuccessor();
            }
            lock (_tickLock)
                EngineState = TEngineState.Running;
        }

        
        public MediaDeleteDenyReason CanDeleteMedia(PersistentMedia media)
        {
            MediaDeleteDenyReason reason = MediaDeleteDenyReason.NoDeny;
            if (media is PersistentMedia && ((PersistentMedia)media).Protected)
                return new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.Protected, Media = media };
            ServerMedia serverMedia = media as ServerMedia;
            if (serverMedia == null)
                return reason;
            else
            {
                foreach (Event e in _rootEvents.ToList())
                {
                    reason = e.CheckCanDeleteMedia(serverMedia);
                    if (reason.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                        return reason;
                }
                return this.DbMediaInUse(serverMedia);
            }
        }

        public IEnumerable<IEvent> GetRootEvents() { return _rootEvents.ToList(); } 

        public void AddRootEvent(IEvent ev)
        {
            _rootEvents.Add(ev as IEventPesistent);
        }

        public IEvent AddNewEvent(
                    UInt64 idRundownEvent = 0,
                    UInt64 idEventBinding = 0,
                    VideoLayer videoLayer = VideoLayer.None,
                    TEventType eventType = TEventType.Rundown,
                    TStartType startType = TStartType.None,
                    TPlayState playState = TPlayState.Scheduled,
                    DateTime scheduledTime = default(DateTime),
                    TimeSpan duration = default(TimeSpan),
                    TimeSpan scheduledDelay = default(TimeSpan),
                    TimeSpan scheduledTC = default(TimeSpan),
                    Guid mediaGuid = default(Guid),
                    string eventName = "",
                    DateTime startTime = default(DateTime),
                    TimeSpan startTC = default(TimeSpan),
                    TimeSpan? requestedStartTime = null,
                    TimeSpan transitionTime = default(TimeSpan),
                    TimeSpan transitionPauseTime = default(TimeSpan), 
                    TTransitionType transitionType = TTransitionType.Cut,
                    TEasing transitionEasing = TEasing.Linear,
                    decimal? audioVolume = null,
                    UInt64 idProgramme = 0,
                    string idAux = "",
                    bool isEnabled = true,
                    bool isHold = false,
                    bool isLoop = false,
                    bool isCGEnabled = false,
                    byte crawl = 0,
                    byte logo = 0,
                    byte parental = 0,
                    AutoStartFlags autoStartFlags = AutoStartFlags.None,
                    string command = null,
                    IDictionary<string, string> fields = null,
                    TemplateMethod method = TemplateMethod.Add,
                    int templateLayer = -1
                    )
        {
            IEvent result;
            if (!_events.TryGetValue(idRundownEvent, out result))
            {
                if (eventType == TEventType.Animation)
                    result = new AnimatedEvent(this, idRundownEvent, idEventBinding, videoLayer, startType, playState, scheduledTime, duration, scheduledDelay, mediaGuid, eventName, startTime, isEnabled, fields, method, templateLayer);
                else if (eventType == TEventType.CommandScript)
                    result = new CommandScriptEvent(this, idRundownEvent, idEventBinding, startType, playState, scheduledDelay, eventName, startTime, isEnabled, command);
                else
                    result = new Event(this, idRundownEvent, idEventBinding, videoLayer, eventType, startType, playState, scheduledTime, duration, scheduledDelay, scheduledTC, mediaGuid, eventName, startTime, startTC, requestedStartTime, transitionTime, transitionPauseTime, transitionType, transitionEasing, audioVolume, idProgramme, idAux, isEnabled, isHold, isLoop, autoStartFlags, isCGEnabled, crawl, logo, parental);
                if (idRundownEvent == 0)
                    result.Save();
                if (_events.TryAdd(((Event)result).IdRundownEvent, result))
                {
                    result.Saved += _eventSaved;
                    result.Deleted += _eventDeleted;
                }
                if (startType == TStartType.OnFixedTime)
                    _fixedTimeEvents.Add(result as Event);
            }
            return result;
        }

        private void _removeEvent(Event aEvent)
        {
            _rootEvents.Remove(aEvent);
            IEvent eventToRemove;
            if (_events.TryRemove(aEvent.IdRundownEvent, out eventToRemove))
            {
                aEvent.Saved -= _eventSaved;
                aEvent.Deleted -= _eventDeleted;
            }
            if (aEvent.StartType == TStartType.OnFixedTime)
                RemoveFixedTimeEvent(aEvent);
            var media = aEvent.Media as ServerMedia;
            if (media != null
                && aEvent.PlayState == TPlayState.Played
                && media.MediaType == TMediaType.Movie
                && ArchivePolicy == TArchivePolicyType.ArchivePlayedAndNotUsedWhenDeleteEvent
                && MediaManager.ArchiveDirectory != null
                && CanDeleteMedia(media).Reason == MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                ThreadPool.QueueUserWorkItem(o => MediaManager.ArchiveMedia(new List<IServerMedia>(new [] { media }), true));
        }

        private TEngineState _engineState;
        [XmlIgnore]
        public TEngineState EngineState
        {
            get { return _engineState; }
            private set
            {
                lock (_runningEvents.SyncRoot)
                if (SetField(ref _engineState, value, nameof(EngineState)))
                    {
                        if (value == TEngineState.Hold)
                            foreach (Event ev in _runningEvents.Where(e => (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) && ((Event)e).IsFinished()).ToList())
                            {
                                _pause(ev, true);
                                Debug.WriteLine(ev, "Hold: Played");
                            }
                        if (value == TEngineState.Idle && _runningEvents.Count > 0)
                        {
                            foreach (Event ev in _runningEvents.Where(e => (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) && ((Event)e).IsFinished()).ToList())
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
                if (SetField(ref _programAudioVolume, value, nameof(ProgramAudioVolume)))
                {
                    var playing = Playing;
                    int transitioDuration = playing == null ? 0 : (int)playing.TransitionTime.ToSMPTEFrames(_frameRate);
                    if (_playoutChannelPRI != null)
                        _playoutChannelPRI.SetVolume(VideoLayer.Program, value, transitioDuration);
                    if (_playoutChannelSEC != null && !(_playoutChannelSEC == _playoutChannelPRV && _previewLoaded))
                        _playoutChannelSEC.SetVolume(VideoLayer.Program, value, transitioDuration);
                }
            }
        }

        bool _fieldOrderInverted;
        [XmlIgnore]
        public bool FieldOrderInverted
        {
            get { return _fieldOrderInverted; }
            set { if (SetField(ref _fieldOrderInverted, value, nameof(FieldOrderInverted)))
                {
                    if (_playoutChannelPRI != null)
                        _playoutChannelPRI.SetFieldOrderInverted(VideoLayer.Program, value);
                    if (_playoutChannelSEC != null && !(_playoutChannelSEC == _playoutChannelPRV && _previewLoaded))
                        _playoutChannelSEC.SetFieldOrderInverted(VideoLayer.Program, value);
                }
            }
        }


        private void _reSchedule(Event aEvent)
        {
            if (aEvent == null)
                return;
            lock (RundownSync)
            {
                try
                {
                    if (aEvent.PlayState == TPlayState.Aborted
                        || aEvent.PlayState == TPlayState.Played)
                    {
                        aEvent.PlayState = TPlayState.Scheduled;
                        foreach (Event se in aEvent.SubEvents)
                            _reSchedule(se);
                    }

                    Event next = aEvent.getSuccessor();
                    if (next != null)
                        _reSchedule(next);
                }
                finally
                {
                    aEvent.Save();
                }
            }
        }


        public void ReSchedule(IEvent aEvent)
        {
            ThreadPool.QueueUserWorkItem(o => {
                try
                {
                    _reSchedule(aEvent as Event);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "ReScheduleDelayed exception");
                }
            });
        }

        public void Execute(string command)
        {
            if (_playoutChannelPRI != null)
                _playoutChannelPRI.Execute(command);
            if (_playoutChannelSEC != null)
                _playoutChannelSEC.Execute(command);
        }

        #endregion // IEngine methods

        #region IEngine properties

        [XmlIgnore]
        public ICollection<IEventPesistent> VisibleEvents { get { return _visibleEvents.Cast<IEventPesistent>().ToList(); } }

        #endregion // IEngine properties


        protected virtual void NotifyEngineOperation(IEvent aEvent, TEngineOperation operation)
        {
            EngineOperation?.Invoke(this, new EngineOperationEventArgs(aEvent, operation));
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> VisibleEventsOperation;
        private void _visibleEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            VisibleEventsOperation?.Invoke(o, new CollectionOperationEventArgs<IEvent>(e.Item, e.Operation));
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> PreloadedEventsOperation;
        private void _loadedNextEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            PreloadedEventsOperation?.Invoke(o, e);
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        private void _runningEventsOperation(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            RunningEventsOperation?.Invoke(sender, new CollectionOperationEventArgs<IEvent>(e.Item, e.Operation));
        }

        public event EventHandler<IEventEventArgs> EventSaved; 
        private void _eventSaved(object sender, EventArgs e)
        {
            EventSaved?.Invoke(this, new IEventEventArgs(sender as IEvent));
        }

        public event EventHandler<IEventEventArgs> EventDeleted;
        private void _eventDeleted(object sender, EventArgs e)
        {
            _removeEvent(sender as Event);
            EventDeleted?.Invoke(this, new IEventEventArgs(sender as IEvent));
            ((IDisposable)sender).Dispose();
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
                if (SetField(ref _pst2Prv, value, nameof(Pst2Prv)))
                {
                    if (value)
                        _loadPST();
                    else
                        if (_playoutChannelPRV != null)
                            _playoutChannelPRV.Clear(VideoLayer.Preset);
                }
            }
        }

    }

}