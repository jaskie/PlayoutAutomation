using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Xml.Serialization;
using TAS.Common;
using System.Collections.Concurrent;
using TAS.Remoting.Server;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TAS.Database;
using TAS.Common.Interfaces;
using TAS.Server.Media;
using TAS.Server.Security;

namespace TAS.Server
{
    public class Engine : DtoBase, IEngine, IEnginePersistent
    {

        private const int PerviewPositionSetDelay = 100;

        private string _engineName;
        private bool _pst2Prv;

        [JsonProperty(nameof(PlayoutChannelPRI), TypeNameHandling = TypeNameHandling.Objects, IsReference = true)]
        private CasparServerChannel _playoutChannelPRI;

        [JsonProperty(nameof(PlayoutChannelSEC), TypeNameHandling = TypeNameHandling.Objects, IsReference = true)]
        private CasparServerChannel _playoutChannelSEC;

        [JsonProperty(nameof(PlayoutChannelPRV), TypeNameHandling = TypeNameHandling.Objects, IsReference = true)]
        private CasparServerChannel _playoutChannelPRV;

        [JsonProperty(nameof(MediaManager), TypeNameHandling = TypeNameHandling.Objects, IsReference = true)]
        private readonly MediaManager _mediaManager;

        [JsonProperty(nameof(AuthenticationService), TypeNameHandling = TypeNameHandling.Objects, IsReference = true)]
        private AuthenticationService _authenticationService;

        Thread _engineThread;
        private long _currentTicks;
        public readonly object RundownSync = new object();
        private readonly object _tickLock = new object();

        private readonly SynchronizedCollection<Event> _visibleEvents = new SynchronizedCollection<Event>()
            ; // list of visible events

        private readonly List<IEvent> _runningEvents = new List<IEvent>(); // list of events loaded and playing 

        private readonly ConcurrentDictionary<VideoLayer, IEvent> _preloadedEvents =
            new ConcurrentDictionary<VideoLayer, IEvent>();

        private readonly SynchronizedCollection<Event> _rootEvents = new SynchronizedCollection<Event>();
        private readonly SynchronizedCollection<Event> _fixedTimeEvents = new SynchronizedCollection<Event>();
        private readonly ConcurrentDictionary<Guid, IEvent> _events = new ConcurrentDictionary<Guid, IEvent>();
        private readonly Lazy<List<IAclRight>> _rights;
        private Event _playing;
        private Event _forcedNext;
        private IEnumerable<IGpi> _localGpis;
        private IEnumerable<IEnginePlugin> _plugins;
        private TimeSpan _timeCorrection;
        private bool _isWideScreen;
        private TEngineState _engineState;
        private double _programAudioVolume = 1;
        private bool _fieldOrderInverted;

        [JsonProperty(nameof(PreviewMedia), TypeNameHandling = TypeNameHandling.Objects, IsReference = true)]
        private IMedia _previewMedia;
        private long _previewDuration;
        private long _previewPosition;
        private long _previewLoadedSeek;
        private double _previewAudioVolume;
        private bool _previewLoaded;
        private bool _previewIsPlaying;
        private CancellationTokenSource _previewPositionCancellationTokenSource;
        private long _previewLastPositionSetTick;


        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(Engine));
        private static TimeSpan _preloadTime = new TimeSpan(0, 0, 2); // time to preload event


        public Engine()
        {
            _engineState = TEngineState.NotInitialized;
            _mediaManager = new MediaManager(this);
            Db.ConnectionStateChanged += _database_ConnectionStateChanged;
            _rights = new Lazy<List<IAclRight>>(() => Db.DbReadEngineAclList<EngineAclRight>(this, AuthenticationService as IAuthenticationServicePersitency));
        }

        public event EventHandler<EngineTickEventArgs> EngineTick;
        public event EventHandler<EngineOperationEventArgs> EngineOperation;
        public event EventHandler<EventEventArgs> VisibleEventAdded;
        public event EventHandler<EventEventArgs> VisibleEventRemoved;

        public event EventHandler<CollectionOperationEventArgs<IEvent>> PreloadedEventsOperation;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        public event EventHandler<EventEventArgs> EventLocated;
        public event EventHandler<EventEventArgs> EventDeleted;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;


        #region IEngineProperties

        // xml-ignored properties are readed from table's fields, rest is from xml ("Config" field)
        [XmlIgnore]
        public ulong Instance { get; set; }

        [XmlIgnore]
        public ulong IdArchive { get; set; }

        [XmlIgnore]
        public ulong IdServerPRI { get; set; }

        [XmlIgnore]
        public int ServerChannelPRI { get; set; }

        [XmlIgnore]
        public ulong IdServerSEC { get; set; }

        [XmlIgnore]
        public int ServerChannelSEC { get; set; }

        [XmlIgnore]
        public ulong IdServerPRV { get; set; }

        [XmlIgnore]
        public int ServerChannelPRV { get; set; }

        public int CGStartDelay { get; set; }

        [JsonProperty]
        public string EngineName
        {
            get { return _engineName; }
            set { SetField(ref _engineName, value); }
        }

        [JsonProperty]
        public bool EnableCGElementsForNewEvents { get; set; }

        [JsonProperty]
        public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }

        #endregion //IEngineProperties

        public TArchivePolicyType ArchivePolicy { get; set; } = TArchivePolicyType.NoArchive;

        [XmlIgnore]
        public IMediaManager MediaManager => _mediaManager;

        [XmlIgnore]
        [JsonProperty]
        public ICGElementsController CGElementsController { get; private set; }

        public ServerHost Remote { get; set; }
        public TAspectRatioControl AspectRatioControl { get; set; }
        public double VolumeReferenceLoudness { get; set; }

        public int TimeCorrection
        {
            get { return (int) _timeCorrection.TotalMilliseconds; }
            set { _timeCorrection = TimeSpan.FromMilliseconds(value); }
        }

        [XmlIgnore]
        public DateTime CurrentTime { get; private set; }

        public DateTime AlignDateTime(DateTime dt)
        {
            return new DateTime((dt.Ticks / FrameTicks) * FrameTicks, dt.Kind);
        }

        public TimeSpan AlignTimeSpan(TimeSpan ts)
        {
            return new TimeSpan((ts.Ticks / FrameTicks) * FrameTicks);
        }

        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPRI => _playoutChannelPRI;

        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelSEC => _playoutChannelSEC;

        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPRV => _playoutChannelPRV;

        [XmlIgnore]
        [JsonProperty]
        public long FrameTicks { get; private set; }

        [XmlIgnore]
        [JsonProperty]
        public RationalNumber FrameRate { get; private set; }

        [JsonProperty]
        public TVideoFormat VideoFormat { get; set; }

        [XmlIgnore]
        [JsonProperty(IsReference = false)]
        public VideoFormatDescription FormatDescription { get; private set; }

        [XmlIgnore]
        [JsonProperty]
        public TEngineState EngineState
        {
            get { return _engineState; }
            private set
            {
                lock (_tickLock)
                    if (SetField(ref _engineState, value))
                    {
                        if (value == TEngineState.Hold)
                            foreach (var ev in _runningEvents.Where(e =>
                                (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) &&
                                ((Event) e).IsFinished()).ToArray())
                            {
                                _pause((Event) ev, true);
                                Debug.WriteLine(ev, "Hold: Played");
                            }
                        if (value == TEngineState.Idle && _runningEvents.Count > 0)
                        {
                            foreach (var ev in _runningEvents.Where(e =>
                                (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) &&
                                ((Event) e).IsFinished()).ToArray())
                            {
                                _pause((Event) ev, true);
                                Debug.WriteLine(ev, "Idle: Played");
                            }
                        }
                    }
            }
        }

        [XmlIgnore]
        [JsonProperty]
        public bool FieldOrderInverted
        {
            get { return _fieldOrderInverted; }
            set
            {
                if (SetField(ref _fieldOrderInverted, value))
                {
                    _playoutChannelPRI?.SetFieldOrderInverted(VideoLayer.Program, value);
                    if (_playoutChannelSEC != null && !(_playoutChannelSEC == _playoutChannelPRV && _previewLoaded))
                        _playoutChannelSEC.SetFieldOrderInverted(VideoLayer.Program, value);
                }
            }
        }

        [XmlIgnore]
        [JsonProperty]
        public double ProgramAudioVolume
        {
            get { return _programAudioVolume; }
            set
            {
                if (SetField(ref _programAudioVolume, value))
                {
                    var playing = Playing;
                    int transitioDuration = playing == null ? 0 : (int) playing.TransitionTime.ToSMPTEFrames(FrameRate);
                    _playoutChannelPRI?.SetVolume(VideoLayer.Program, value, transitioDuration);
                    if (_playoutChannelSEC != null && !(_playoutChannelSEC == _playoutChannelPRV && _previewLoaded))
                        _playoutChannelSEC.SetVolume(VideoLayer.Program, value, transitioDuration);
                }
            }
        }

        public void Initialize(IList<CasparServer> servers, AuthenticationService authenticationService)
        {
            Debug.WriteLine(this, "Begin initializing");
            Logger.Debug("Initializing engine {0}", this);
            _authenticationService = authenticationService;

            var recorders = new List<CasparRecorder>();

            var sPRI = servers.FirstOrDefault(s => s.Id == IdServerPRI);
            _playoutChannelPRI = (CasparServerChannel) sPRI?.Channels.FirstOrDefault(c => c.Id == ServerChannelPRI);
            if (sPRI != null)
                recorders.AddRange(sPRI.Recorders.Select(r => r as CasparRecorder));
            var sSEC = servers.FirstOrDefault(s => s.Id == IdServerSEC);
            if (sSEC != null && sSEC != sPRI)
                recorders.AddRange(sSEC.Recorders.Select(r => r as CasparRecorder));
            _playoutChannelSEC = (CasparServerChannel) sSEC?.Channels.FirstOrDefault(c => c.Id == ServerChannelSEC);
            var sPRV = servers.FirstOrDefault(s => s.Id == IdServerPRV);
            if (sPRV != null && sPRV != sPRI && sPRV != sSEC)
                recorders.AddRange(sPRV.Recorders.Select(r => r as CasparRecorder));
            _playoutChannelPRV = (CasparServerChannel) sPRV?.Channels.FirstOrDefault(c => c.Id == ServerChannelPRV);
            _mediaManager.SetRecorders(recorders);


            _localGpis = this.ComposeParts<IGpi>();
            _plugins = this.ComposeParts<IEnginePlugin>();
            CGElementsController = this.ComposePart<ICGElementsController>();

            FormatDescription = VideoFormatDescription.Descriptions[VideoFormat];
            FrameTicks = FormatDescription.FrameTicks;
            FrameRate = FormatDescription.FrameRate;
            var chPRI = PlayoutChannelPRI as CasparServerChannel;
            var chSEC = PlayoutChannelSEC as CasparServerChannel;
            if (chSEC != null
                && chSEC != chPRI)
            {
                chSEC.Owner.Initialize(_mediaManager);
                chSEC.Owner.MediaDirectory.DirectoryName = chSEC.ChannelName;
                chSEC.Owner.PropertyChanged += _server_PropertyChanged;
            }

            if (chPRI != null)
            {
                chPRI.Owner.Initialize(_mediaManager);
                chPRI.Owner.MediaDirectory.DirectoryName = chPRI.ChannelName;
                chPRI.Owner.PropertyChanged += _server_PropertyChanged;
            }

            _mediaManager.Initialize();

            Debug.WriteLine(this, "Reading Root Events");
            this.DbReadRootEvents();

            EngineState = TEngineState.Idle;
            var cgElementsController = CGElementsController;
            if (cgElementsController != null)
            {
                Debug.WriteLine(this, "Initializing CGElementsController");
                cgElementsController.Started += _gpiStartLoaded;
            }

            if (Remote != null)
            {
                Debug.WriteLine(this, "Initializing Remote interface");
                Remote.Initialize(this, "/Engine", _authenticationService);
            }

            if (_localGpis != null)
                foreach (var gpi in _localGpis)
                    gpi.Started += _gpiStartLoaded;

            Debug.WriteLine(this, "Creating engine thread");
            _engineThread = new Thread(_engineThreadProc);
            _engineThread.Priority = ThreadPriority.Highest;
            _engineThread.Name = $"Engine main thread for {EngineName}";
            _engineThread.IsBackground = true;
            _engineThread.Start();
            Debug.WriteLine(this, "Engine initialized");
            Logger.Debug("Engine {0} initialized", this);
        }

        [JsonProperty]
        public ConnectionStateRedundant DatabaseConnectionState { get; } = Db.ConnectionState;

        [XmlIgnore]
        public List<IEvent> FixedTimeEvents
        {
            get
            {
                lock (_fixedTimeEvents.SyncRoot) return _fixedTimeEvents.Cast<IEvent>().ToList();
            }
        }

        [XmlIgnore]
        public ICollection<IEventPesistent> VisibleEvents => _visibleEvents.Cast<IEventPesistent>().ToList();

        [XmlIgnore]
        public IEvent Playing
        {
            get { return _playing; }
            private set
            {
                var oldPlaying = _playing;
                if (!SetField(ref _playing, (Event) value))
                    return;
                if (oldPlaying != null)
                    oldPlaying.SubEventChanged -= _playingSubEventsChanged;
                if (value != null)
                {
                    value.SubEventChanged += _playingSubEventsChanged;
                    var media = value.Media;
                    SetField(ref _fieldOrderInverted, media?.FieldOrderInverted ?? false, nameof(FieldOrderInverted));
                }
            }
        }

        public IEvent GetNextToPlay()
        {
            var e = _playing;
            if (e == null)
                return null;
            e = _successor(e);
            if (e == null)
                return null;
            if (e.EventType == TEventType.Rundown)
                return e.FindVisibleSubEvent();
            return e;
        }

        public IEvent GetNextWithRequestedStartTime()
        {
                var e = _playing;
                if (e == null)
                    return null;
                do
                    e = e.GetEnabledSuccessor();
                while (e != null && e.RequestedStartTime == null);
                return e;
        }

        [XmlIgnore, JsonProperty]
        public IEvent ForcedNext
        {
            get { return _forcedNext; }
            private set
            {

                lock (_tickLock)
                {
                    var oldForcedNext = _forcedNext;
                    if (SetField(ref _forcedNext, (Event)value))
                    {
                        Debug.WriteLine(value, "ForcedNext");
                        NotifyPropertyChanged(nameof(GetNextToPlay));
                        if (_forcedNext != null)
                            _forcedNext.IsForcedNext = true;
                        if (oldForcedNext != null)
                            oldForcedNext.IsForcedNext = false;
                    }
                }
            }
        }

        [XmlIgnore, JsonProperty]
        public bool IsWideScreen
        {
            get { return _isWideScreen; }
            private set
            {
                if (SetField(ref _isWideScreen, value))
                    if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                    {
                        _playoutChannelPRI?.SetAspect(VideoLayer.Program, !value);
                        _playoutChannelSEC?.SetAspect(VideoLayer.Program, !value);
                    }
            }
        }

        [XmlIgnore, JsonProperty]
        public bool Pst2Prv
        {
            get { return _pst2Prv; }
            set
            {
                if (SetField(ref _pst2Prv, value))
                {
                    if (value)
                        _loadPST();
                    else
                        _playoutChannelPRV?.Clear(VideoLayer.Preset);
                }
            }
        }

        [XmlIgnore]
        public IMedia PreviewMedia => _previewMedia;

        [XmlIgnore, JsonProperty]
        public long PreviewLoadedSeek => _previewLoadedSeek;

        [XmlIgnore]
        [JsonProperty]
        public long PreviewPosition // from 0 to duration
        {
            get => _previewPosition;
            set
            {
                if (_playoutChannelPRV == null || _previewMedia == null)
                    return;
                if (_previewIsPlaying)
                    PreviewPause();
                long newSeek = value < 0 ? 0 : value;
                long maxSeek = _previewDuration-1;
                if (newSeek > maxSeek)
                    newSeek = maxSeek;
                if (SetField(ref _previewPosition, newSeek))
                {
                    _previewPositionCancellationTokenSource?.Cancel();
                    var cancellationTokenSource = new  CancellationTokenSource();
                    Task.Run(() =>
                    {
                        Thread.Sleep(PerviewPositionSetDelay);
                        if (!cancellationTokenSource.IsCancellationRequested ||
                            _currentTicks > _previewLastPositionSetTick + TimeSpan.TicksPerMillisecond * PerviewPositionSetDelay * 3)
                        {
                            _previewLastPositionSetTick = _currentTicks;
                            _playoutChannelPRV.Seek(VideoLayer.Preview, _previewLoadedSeek + newSeek);
                        }
                    }, cancellationTokenSource.Token);
                    _previewPositionCancellationTokenSource = cancellationTokenSource;
                }
            }
        }

        [XmlIgnore, JsonProperty]
        public double PreviewAudioVolume
        {
            get => _previewAudioVolume;
            set
            {
                if (SetField(ref _previewAudioVolume, value))
                    _playoutChannelPRV.SetVolume(VideoLayer.Preview, (double)Math.Pow(10, (double)value / 20), 0);
            }
        }
        
        [XmlIgnore]
        [JsonProperty]
        public bool PreviewLoaded {
            get => _previewLoaded;
            private set
            {
                if (SetField(ref _previewLoaded, value))
                {
                    var vol = _previewLoaded ? 0 : _programAudioVolume;
                    _playoutChannelPRV?.SetVolume(VideoLayer.Program, vol, 0);
                }
            }
        }

        [XmlIgnore]
        [JsonProperty]
        public bool PreviewIsPlaying
        {
            get => _previewIsPlaying;
            private set => SetField(ref _previewIsPlaying, value);
        }

        public void Load(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Debug.WriteLine(aEvent, "Load");
            lock (_tickLock)
            {
                EngineState = TEngineState.Hold;
                foreach (var e in _visibleEvents.ToList())
                    _stop(e);
                _clearRunning();
            }
            _load(aEvent as Event);
        }

        public void StartLoaded()
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Debug.WriteLine("StartLoaded executed");
            _startLoaded();
        }

        public void Start(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Debug.WriteLine(aEvent, "Start");
            var ets = aEvent as Event;
            if (ets == null)
                return;
            _start(ets);
        }

        public void Schedule(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Debug.WriteLine(aEvent, $"Schedule {aEvent.PlayState}");
            lock (_tickLock)
            {
                EngineState = TEngineState.Running;
                _run((Event) aEvent);
            }
            NotifyEngineOperation(aEvent, TEngineOperation.Schedule);
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Debug.WriteLine(aVideoLayer, "Clear");
            Logger.Info("{0} {1}: Clear layer {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this, aVideoLayer);
            Event ev;
            lock (_visibleEvents.SyncRoot)
                ev = _visibleEvents.FirstOrDefault(e => e.Layer == aVideoLayer);
            lock (_tickLock)
            {
                if (ev != null)
                {
                    ev.PlayState = ev.Position == 0 ? TPlayState.Scheduled : TPlayState.Aborted;
                    ev.SaveDelayed();
                    RemoveVisibleEvent(ev);
                    _runningEvents.Remove(ev);
                    RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(ev, CollectionOperation.Remove));
                }
                _playoutChannelPRI?.Clear(aVideoLayer);
                _playoutChannelSEC?.Clear(aVideoLayer);
            }
            if (aVideoLayer == VideoLayer.Program)
                lock (_tickLock)
                    Playing = null;
        }

        public void Clear()
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Logger.Info("{0} {1}: Clear all", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this);
            lock (_tickLock)
            {
                _clearRunning();
                _visibleEvents.Clear();
                ForcedNext = null;
                _playoutChannelPRI?.Clear();
                _playoutChannelSEC?.Clear();
                ProgramAudioVolume = 1;
                EngineState = TEngineState.Idle;
                Playing = null;
            }
            NotifyEngineOperation(null, TEngineOperation.Clear);
            _previewUnload();
        }

        public void ClearMixer()
        {
            if (!HaveRight(EngineRight.Play))
                return;

            _playoutChannelPRI?.ClearMixer();
            _playoutChannelSEC?.ClearMixer();
        }

        public void Restart()
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Logger.Info("{0} {1}: Restart", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this);
            foreach (var e in _visibleEvents.ToList())
                _restartEvent(e);
        }

        public void RestartRundown(IEvent aRundown)
        {
            if (!HaveRight(EngineRight.Play))
                return;
            lock (_tickLock)
            {
                _restartRundown(aRundown);
                EngineState = TEngineState.Running;
            }
        }

        public void ForceNext(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;
            ForcedNext = aEvent;
        }

        public MediaDeleteResult CanDeleteMedia(PersistentMedia media)
        {
            MediaDeleteResult reason = MediaDeleteResult.NoDeny;
            if (media.Protected)
                return new MediaDeleteResult { Result = MediaDeleteResult.MediaDeleteResultEnum.Protected, Media = media };
            ServerMedia serverMedia = media as ServerMedia;
            if (serverMedia == null)
                return reason;
            foreach (Event e in _rootEvents.ToList())
            {
                reason = e.CheckCanDeleteMedia(serverMedia);
                if (reason.Result != MediaDeleteResult.MediaDeleteResultEnum.Success)
                    return reason;
            }
            return this.DbMediaInUse(serverMedia);
        }

        public IEnumerable<IEvent> GetRootEvents() { lock (_rootEvents.SyncRoot) return _rootEvents.Cast<IEvent>().ToList(); }

        public void AddRootEvent(IEvent aEvent)
        {
            var ev = aEvent as Event;
            if (ev == null)
                return;
            _rootEvents.Add(ev);
            EventLocated?.Invoke(this, new EventEventArgs(ev));
        }

        public IEvent CreateNewEvent(
            ulong idRundownEvent = 0,
            ulong idEventBinding = 0,
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
            double? audioVolume = null,
            ulong idProgramme = 0,
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
            if (eventType == TEventType.Animation)
                result = new AnimatedEvent(this, idRundownEvent, idEventBinding, videoLayer, startType, playState, scheduledTime, duration, scheduledDelay, mediaGuid, eventName, startTime, isEnabled, fields, method, templateLayer);
            else if (eventType == TEventType.CommandScript)
                result = new CommandScriptEvent(this, idRundownEvent, idEventBinding, startType, playState, scheduledDelay, eventName, startTime, isEnabled, command);
            else
                result = new Event(this, idRundownEvent, idEventBinding, videoLayer, eventType, startType, playState, scheduledTime, duration, scheduledDelay, scheduledTC, mediaGuid, eventName, startTime, startTC, requestedStartTime, transitionTime, transitionPauseTime, transitionType, transitionEasing, audioVolume, idProgramme, idAux, isEnabled, isHold, isLoop, autoStartFlags, isCGEnabled, crawl, logo, parental);
            if (_events.TryAdd(((Event)result).DtoGuid, result))
            {
                result.Located += _eventLocated;
                result.Deleted += _eventDeleted;
            }
            if (startType == TStartType.OnFixedTime)
                _fixedTimeEvents.Add((Event)result);
            return result;
        }

        public void ReSchedule(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;

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
            _playoutChannelPRI?.Execute(command);
            _playoutChannelSEC?.Execute(command);
        }

        public void PreviewLoad(IMedia media, long seek, long duration, long position, double previewAudioVolume)
        {
            if (!HaveRight(EngineRight.Preview))
                return;
            MediaBase mediaToLoad = _findPreviewMedia(media as MediaBase);
            Debug.WriteLine(mediaToLoad, "Loading");
            if (mediaToLoad != null)
            {
                _previewDuration = duration;
                _previewLoadedSeek = seek;
                _previewPosition = position;
                _previewMedia = media;
                _previewLastPositionSetTick = _currentTicks;
                _playoutChannelPRV.SetAspect(VideoLayer.Preview, media.VideoFormat == TVideoFormat.NTSC
                                                                 || media.VideoFormat == TVideoFormat.PAL
                                                                 || media.VideoFormat == TVideoFormat.PAL_P);
                PreviewLoaded = true;
                PreviewAudioVolume = previewAudioVolume;
                _playoutChannelPRV.Load(mediaToLoad, VideoLayer.Preview, seek + position, duration - position);
                PreviewIsPlaying = false;
                NotifyPropertyChanged(nameof(PreviewMedia));
                NotifyPropertyChanged(nameof(PreviewPosition));
                NotifyPropertyChanged(nameof(PreviewLoadedSeek));
            }
        }

        public void PreviewUnload()
        {
            if (!HaveRight(EngineRight.Preview))
                return;
            _previewUnload();
        }

        public void PreviewPlay()
        {
            if (!HaveRight(EngineRight.Preview))
                return;
            if (_previewMedia != null && _playoutChannelPRV?.Play(VideoLayer.Preview) == true)
                PreviewIsPlaying = true;
        }

        public void PreviewPause()
        {
            if (!HaveRight(EngineRight.Preview))
                return;
            _playoutChannelPRV?.Pause(VideoLayer.Preview);
           PreviewIsPlaying = false;
        }

        public void SearchMissingEvents()
        {
            this.DbSearchMissing();
        }

        #region  IPersistent properties
        [XmlIgnore]
        public ulong Id { get; set; }

        [XmlIgnore]
        public IAuthenticationService AuthenticationService => _authenticationService;

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IAclObject

        public IEnumerable<IAclRight> GetRights()
        {
            lock (_rights) return _rights.Value.AsReadOnly();
        }

        public IAclRight AddRightFor(ISecurityObject securityObject)
        {
            var right = new EngineAclRight { Owner = this, SecurityObject = securityObject };
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

        public bool HaveRight(EngineRight right)
        {
            IUser identity = Thread.CurrentPrincipal.Identity as IUser;
            if (identity == null)
                return false;
            if (identity.IsAdmin)
                return true; // Full rights
            var groups = identity.GetGroups();
            lock (_rights)
            {
                return _rights.Value.Any(r => r.SecurityObject == identity && (r.Acl & (ulong)right) > 0) 
                    || groups.Any(g => _rights.Value.Any(r => r.SecurityObject == g && (r.Acl & (ulong)right) > 0));
            }
        }

        // internal methods
        internal void UnInitialize()
        {
            Debug.WriteLine(this, "Aborting engine thread");
            _engineThread.Abort();
            _engineThread.Join();
            EngineState = TEngineState.NotInitialized;

            var chPRI = PlayoutChannelPRI as CasparServerChannel;
            var chSEC = PlayoutChannelSEC as CasparServerChannel;
            if (chSEC != null
                && chSEC != chPRI)
                chSEC.Owner.PropertyChanged -= _server_PropertyChanged;
            if (chPRI != null)
                chPRI.Owner.PropertyChanged -= _server_PropertyChanged;

            if (Remote != null)
            {
                Debug.WriteLine(this, "UnInitializing Remote interface");
                Remote.UnInitialize();
            }
            if (_localGpis != null)
                foreach (var gpi in _localGpis)
                    gpi.Started -= _gpiStartLoaded;

            var cgElementsController = CGElementsController;
            if (cgElementsController != null)
            {
                Debug.WriteLine(this, "Uninitializing CGElementsController");
                cgElementsController.Started -= _gpiStartLoaded;
                cgElementsController.Dispose();
            }

            Debug.WriteLine(this, "Engine uninitialized");
        }
        internal void AddFixedTimeEvent(Event e)
        {
            _fixedTimeEvents.Add(e);
            FixedTimeEventOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, CollectionOperation.Add));
        }
        internal void RemoveFixedTimeEvent(Event e)
        {
            if (_fixedTimeEvents.Remove(e))
            {
                FixedTimeEventOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, CollectionOperation.Remove));
            }
        }

        // private methods
        private void _start(Event aEvent)
        {
            lock (_tickLock)
            {
                EngineState = TEngineState.Running;
                var eventsToStop = _visibleEvents.Where(e => e.PlayState == TPlayState.Played || e.PlayState == TPlayState.Playing).ToList();
                _clearRunning();
                _play(aEvent, true);
                foreach (var e in eventsToStop)
                    _stop(e);
            }
        }

        private void _removeEvent(Event aEvent)
        {
            _rootEvents.Remove(aEvent);
            IEvent eventToRemove;
            if (_events.TryRemove(aEvent.DtoGuid, out eventToRemove))
            {
                aEvent.Located -= _eventLocated;
                aEvent.Deleted -= _eventDeleted;
            }
            if (aEvent.StartType == TStartType.OnFixedTime)
                RemoveFixedTimeEvent(aEvent);
            var media = aEvent.Media as ServerMedia;
            if (media != null
                && aEvent.PlayState == TPlayState.Played
                && media.MediaType == TMediaType.Movie
                && ArchivePolicy == TArchivePolicyType.ArchivePlayedAndNotUsedWhenDeleteEvent
                && _mediaManager.ArchiveDirectory != null
                && CanDeleteMedia(media).Result == MediaDeleteResult.MediaDeleteResultEnum.Success)
                ThreadPool.QueueUserWorkItem(o => _mediaManager.ArchiveMedia(new List<IServerMedia>(new[] { media }), true));
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

                    Event next = aEvent.GetEnabledSuccessor();
                    if (next != null)
                        _reSchedule(next);
                }
                finally
                {
                    aEvent.Save();
                }
            }
        }

        private void _load(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetEnabledSuccessor();
            if (aEvent == null)
                return;
            Debug.WriteLine("{0} Load: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), aEvent);
            Logger.Info("{0} {1}: Load {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this, aEvent);
            var eventType = aEvent.EventType;
            if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
            {
                _playoutChannelPRI?.Load(aEvent);
                _playoutChannelSEC?.Load(aEvent);
                AddVisibleEvent(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                    Playing = aEvent;
            }
            _run(aEvent);
            aEvent.PlayState = TPlayState.Paused;
            NotifyEngineOperation(aEvent, TEngineOperation.Load);
            foreach (Event se in (aEvent.SubEvents.Where(e => e.ScheduledDelay == TimeSpan.Zero)))
                _load(se);
        }

        private void _loadNext(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetEnabledSuccessor();
            if (aEvent == null)
                return;
            var eventType = aEvent.EventType;
            IEvent preloaded;
            if ((eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage) && 
                !(_preloadedEvents.TryGetValue(aEvent.Layer, out preloaded) && preloaded == aEvent))
            {
                Debug.WriteLine("{0} LoadNext: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), aEvent);
                Logger.Info("{0} {1}: Preload {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this, aEvent);
                _preloadedEvents[aEvent.Layer] = aEvent;
                _playoutChannelPRI?.LoadNext(aEvent);
                _playoutChannelSEC?.LoadNext(aEvent);
                var cgElementsController = CGElementsController;
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
        }

        private void _play(Event aEvent, bool fromBeginning)
        {
            if (aEvent == null)
                return;
            var eventType = aEvent.EventType;
            if (!aEvent.IsEnabled || (aEvent.Length == TimeSpan.Zero && eventType != TEventType.Animation && eventType != TEventType.CommandScript))
                aEvent = aEvent.GetEnabledSuccessor();
            Debug.WriteLine("{0} Play: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), aEvent.EventName);
            Logger.Info("{0} {1}: Play {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this, aEvent.EventName);
            eventType = aEvent.EventType;
            if (aEvent == _forcedNext)
            {
                ForcedNext = null;
                _runningEvents.ToList().ForEach(
                    e =>
                    {
                        if (e.PlayState == TPlayState.Playing)
                        {
                            ((Event)e).PlayState = ((Event)e).IsFinished() ? TPlayState.Played : TPlayState.Aborted;
                            _runningEvents.Remove(e);
                            RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, CollectionOperation.Remove));
                        }
                        e.SaveDelayed();
                    });                        
            }
            _run(aEvent);
            if (fromBeginning)
                aEvent.Position = 0;
            if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
            {
                _playoutChannelPRI?.Play(aEvent);
                _playoutChannelSEC?.Play(aEvent);
                AddVisibleEvent(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                {
                    Playing = aEvent;
                    ProgramAudioVolume = (double)Math.Pow(10, (double)aEvent.GetAudioVolume() / 20); ;
                    _setAspectRatio(aEvent);
                    var cgController = CGElementsController;
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
                _playoutChannelPRI?.Play((Event)aEvent);
                _playoutChannelSEC?.Play((Event)aEvent);
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
                ThreadPool.QueueUserWorkItem(o => aEvent.AsRunLogWrite());
        }

        private void _startLoaded()
        {
            lock (_tickLock)
                if (EngineState == TEngineState.Hold)
                {
                    _visibleEvents.Where(e => e.PlayState == TPlayState.Played).ToList().ForEach(e => _stop(e));
                    foreach (var e in _runningEvents.ToArray())
                    {
                        if (!(e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading))
                            _play((Event)e, false);
                    }
                    EngineState = TEngineState.Running;
                }
        }

        private void _clearRunning()
        {
            Debug.WriteLine("_clearRunning");
            foreach (var e in _runningEvents.ToArray())
            {
                _runningEvents.Remove(e);
                ((Event)e).PlayState = ((Event) e).Position == 0 ? TPlayState.Scheduled : TPlayState.Aborted;
                RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, CollectionOperation.Remove));
                e.SaveDelayed();
            }
        }

        private void _setAspectRatio(Event aEvent)
        {
            if (aEvent == null || !(aEvent.Layer == VideoLayer.Program || aEvent.Layer == VideoLayer.Preset))
                return;
            var media = aEvent.Media;
            var narrow = media != null && (media.VideoFormat == TVideoFormat.PAL || media.VideoFormat == TVideoFormat.NTSC || media.VideoFormat == TVideoFormat.PAL_P);
            IsWideScreen = !narrow;
            if (AspectRatioControl != TAspectRatioControl.GPI &&
                AspectRatioControl != TAspectRatioControl.GPIandImageResize)
                return;
            var cgController = CGElementsController;
            if (cgController?.IsConnected == true && cgController.IsCGEnabled)
                cgController.IsWideScreen = !narrow;
            var lGpis = _localGpis;
            if (lGpis != null)
                foreach (var gpi in lGpis)
                    gpi.IsWideScreen = !narrow;
        }

        private void _run(Event aEvent)
        {
            var eventType = aEvent.EventType;
            if (eventType == TEventType.Animation || eventType == TEventType.CommandScript || _runningEvents.Contains(aEvent))
                return;
            _runningEvents.Add(aEvent);
            RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(aEvent, CollectionOperation.Add));
        }

        private void _stop(Event aEvent)
        {
            aEvent.PlayState = aEvent.Position == 0 ? TPlayState.Scheduled : aEvent.IsFinished() ? TPlayState.Played : TPlayState.Aborted;
            aEvent.SaveDelayed();
            lock (_visibleEvents.SyncRoot)
                if (_visibleEvents.Contains(aEvent))
                {
                    var eventType = aEvent.EventType;
                    if (eventType != TEventType.Live && eventType != TEventType.CommandScript)
                    {
                        Debug.WriteLine("{0} Stop: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), aEvent.EventName);
                        Logger.Info("{0} {1}: Stop {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this, aEvent.EventName);
                        _playoutChannelPRI?.Stop(aEvent);
                        _playoutChannelSEC?.Stop(aEvent);
                    }
                    RemoveVisibleEvent(aEvent);
                }
            _runningEvents.Remove(aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Stop);
        }

        private void _pause(Event aEvent, bool finish)
        {
            lock (_visibleEvents.SyncRoot)
                if (_visibleEvents.Contains(aEvent))
                {
                    Debug.WriteLine("{0} Pause: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), aEvent.EventName);
                    Logger.Info("{0} {1}: Pause {2}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(FrameRate), this, aEvent.EventName);
                    if (aEvent.EventType != TEventType.Live && aEvent.EventType != TEventType.StillImage && aEvent is Event)
                    {
                        _playoutChannelPRI?.Pause((Event)aEvent);
                        _playoutChannelSEC?.Pause((Event)aEvent);
                    }
                    foreach (Event se in aEvent.SubEvents)
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
            Event ev = GetNextToPlay() as Event;
            if (ev != null && PlayoutChannelPRV != null)
            {
                MediaBase media = ev.ServerMediaPRV;
                if (media != null)
                {
                    _playoutChannelPRV.Load(media, VideoLayer.Preset, 0, -1);
                    return;
                }
            }
            _playoutChannelPRV.Load(System.Drawing.Color.Black, VideoLayer.Preset);
        }

        private void _previewUnload()
        {
            var channel = _playoutChannelPRV;
            var media = _previewMedia;
            if (channel == null || media == null)
                return;
            _previewPositionCancellationTokenSource?.Cancel();
            channel.Clear(VideoLayer.Preview);
            lock (_tickLock)
            {
                _previewDuration = 0;
                _previewPosition = 0;
                _previewLoadedSeek = 0;
                _previewMedia = null;
            }
            PreviewLoaded = false;
            PreviewIsPlaying = false;
            NotifyPropertyChanged(nameof(PreviewMedia));
            NotifyPropertyChanged(nameof(PreviewPosition));
            NotifyPropertyChanged(nameof(PreviewLoadedSeek));
        }

        private void _restartEvent(Event ev)
        {
            if (ev == null)
                return;
            _playoutChannelPRI?.ReStart(ev);
            _playoutChannelSEC?.ReStart(ev);
        }

        private void _restartRundown(IEvent aRundown)
        {
            Action<Event> rerun = aEvent =>
            {
                _run(aEvent);
                if (aEvent.EventType != TEventType.Rundown)
                {
                    AddVisibleEvent(aEvent);
                    _restartEvent(aEvent);
                }
            };

            var ev = aRundown as Event;
            while (ev != null)
            {
                if (_currentTicks >= ev.ScheduledTime.Ticks &&
                    _currentTicks < ev.ScheduledTime.Ticks + ev.Duration.Ticks)
                {
                    ev.Position = (_currentTicks - ev.ScheduledTime.Ticks) / FrameTicks;
                    var st = ev.StartTime;
                    ev.PlayState = TPlayState.Playing;
                    ev.StartTime = st;
                    rerun(ev);
                    foreach (var se in ev.SubEvents)
                        _restartRundown(se);
                    break;
                }
                ev = ev.GetEnabledSuccessor();
            }
        }

        private void _tick(long nFrames)
        {
            lock (_tickLock)
            {
                if (EngineState == TEngineState.Running)
                {
                        foreach (var e in _runningEvents.Where(ev => ev.PlayState == TPlayState.Playing || ev.PlayState == TPlayState.Fading))
                            ((Event)e).Position += nFrames;

                    Event playingEvent = _playing;
                    Event succEvent = null;
                    if (playingEvent != null)
                    {
                        succEvent = _successor(playingEvent);
                        if (succEvent != null)
                        {
                            if (playingEvent.Position * FrameTicks >= playingEvent.Duration.Ticks - succEvent.TransitionTime.Ticks)
                            {
                                if (playingEvent.PlayState == TPlayState.Playing)
                                    playingEvent.PlayState = TPlayState.Fading;
                            }
                            if (playingEvent.Position * FrameTicks >= playingEvent.Duration.Ticks - _preloadTime.Ticks)
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
                            TimeSpan playingEventPosition = TimeSpan.FromTicks(playingEvent.Position * FrameTicks);
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

                    IEnumerable<IEvent> runningEvents = _runningEvents.Where(e => e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading).ToList();
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
                                                                    _currentTicks >= e.ScheduledTime.Ticks && _currentTicks < e.ScheduledTime.Ticks + TimeSpan.TicksPerSecond) // auto start only within 1 second slot
                    );
                if (startEvent == null)
                    return;
                _runningEvents.Remove(startEvent);
                RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(startEvent, CollectionOperation.Remove));
                startEvent.PlayState = TPlayState.Scheduled;
                _start(startEvent);
            }
        }

        private Event _successor(Event playingEvent)
        {
            var result = _forcedNext;
            if (result != null)
                return result;
            if (playingEvent == null)
                return null;
            result = (playingEvent.IsLoop ? playingEvent : playingEvent.GetEnabledSuccessor()) ?? playingEvent.GetVisualRootTrack().FirstOrDefault(e => e.IsLoop) as Event;
            return result;
        }

        private void _playingSubEventsChanged(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (_playing != sender)
                return;
            if (e.Operation == CollectionOperation.Remove)
                _stop((Event)e.Item);
            else
            {
                lock (_tickLock)
                {
                    var ps = ((Event)sender).PlayState;
                    if (ps != TPlayState.Playing && ps != TPlayState.Paused ||
                        e.Item.PlayState != TPlayState.Scheduled)
                        return;
                    ((Event)e.Item).Position = ((Event)sender).Position;
                    if (ps == TPlayState.Paused)
                    {
                        if (e.Item.EventType == TEventType.StillImage)
                            _load((Event)e.Item);
                    }
                    else
                        _play((Event)e.Item, false);
                }
            }
        }

        private TimeSpan _getTimeToAttention()
        {
            var pe = _playing;
            if (pe == null || (pe.PlayState != TPlayState.Playing && pe.PlayState != TPlayState.Paused))
                return TimeSpan.Zero;
            var result = pe.Length - TimeSpan.FromTicks(pe.Position * FrameTicks);
            pe = pe.GetEnabledSuccessor();
            while (pe != null)
            {
                var pauseTime = pe.GetAttentionTime();
                if (pauseTime != null)
                    return result + pauseTime.Value - pe.TransitionTime;
                result = result + pe.Length - pe.TransitionTime;
                pe = pe.GetEnabledSuccessor();
            }
            return result;
        }

        private void _database_ConnectionStateChanged(object sender, RedundantConnectionStateEventArgs e)
        {
            NotifyPropertyChanged(nameof(DatabaseConnectionState));
            Logger.Error("Database state changed from {0} to {1}. Stack trace was {2}", e.OldState, e.NewState, new StackTrace());
        }
        
        protected override void DoDispose()
        {
            foreach (var e in _rootEvents)
                e.SaveLoadedTree();
            Db.ConnectionStateChanged -= _database_ConnectionStateChanged;
            CGElementsController?.Dispose();
            Remote?.Dispose();
            base.DoDispose();
        }

        private void NotifyEngineOperation(IEvent aEvent, TEngineOperation operation)
        {
            EngineOperation?.Invoke(this, new EngineOperationEventArgs(aEvent, operation));
        }

        private void NotifyLoadedNextEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            PreloadedEventsOperation?.Invoke(o, e);
        }

        private void _engineThreadProc()
        {
            Debug.WriteLine(this, "Engine thread started");
            Logger.Debug("Started engine thread for {0}", this);
            CurrentTime = AlignDateTime(DateTime.UtcNow + _timeCorrection);
            _currentTicks = CurrentTime.Ticks;

            var playingEvents = this.DbSearchPlaying().Cast<Event>().ToArray();
            var playing = playingEvents.FirstOrDefault(e => e.Layer == VideoLayer.Program && (e.EventType == TEventType.Live || e.EventType == TEventType.Movie));
            if (playing != null)
            {
                Debug.WriteLine(playing, "Playing event found");
                if (_currentTicks < (playing.ScheduledTime + playing.Duration).Ticks)
                {
                    foreach (var e in playingEvents)
                    {
                        e.Position = (_currentTicks - e.ScheduledTime.Ticks) / FrameTicks;
                        _run(e);
                        AddVisibleEvent(e);
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

            ulong currentTime;
            ulong frameDuration = (ulong)FrameTicks;
            QueryUnbiasedInterruptTime(out currentTime);
            ulong prevTime = currentTime - frameDuration;
            while (!IsDisposed)
            {
                try
                {
                    CurrentTime = AlignDateTime(DateTime.UtcNow + _timeCorrection);
                    QueryUnbiasedInterruptTime(out currentTime);
                    _currentTicks = CurrentTime.Ticks;
                    ulong nFrames = (currentTime - prevTime) / frameDuration;
                    prevTime += (nFrames * frameDuration);
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
                    Debug.WriteLineIf(nFrames == 0, "Zero frames tick");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e, "Exception in engine tick");
                    Logger.Error($"{e}");
                }
                QueryUnbiasedInterruptTime(out currentTime);
                int waitTime = (int)((prevTime + frameDuration - currentTime + 10000) / 10000);
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

        private void _gpiStartLoaded(object o, EventArgs e)
        {
            _startLoaded();
        }
        
        private void _server_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<CasparServerChannel, List<Event>> channelConnected = (channel, ve) =>
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
                if (sender == ((CasparServerChannel)PlayoutChannelPRI)?.Owner)
                    channelConnected(_playoutChannelPRI, ve);
                if (sender == ((CasparServerChannel)PlayoutChannelSEC)?.Owner
                    && PlayoutChannelSEC != PlayoutChannelPRI)
                    channelConnected(_playoutChannelSEC, ve);
            }
        }

        private void _eventDeleted(object sender, EventArgs e)
        {
            _removeEvent(sender as Event);
            EventDeleted?.Invoke(this, new EventEventArgs(sender as IEvent));
            ((IDisposable)sender).Dispose();
        }

        private void _eventLocated(object sender, EventArgs e)
        {
            EventLocated?.Invoke(this, new EventEventArgs(sender as IEvent));
        }


        private MediaBase _findPreviewMedia(MediaBase media)
        {
            var playoutChannel = _playoutChannelPRV;
            if (!(media is ServerMedia))
                return media;
            if (playoutChannel == null)
                return null;
            return media.Directory == playoutChannel.Owner.MediaDirectory
                ? media
                : ((ServerDirectory)playoutChannel.Owner.MediaDirectory).FindMediaByMediaGuid(media.MediaGuid);
        }

        private void AddVisibleEvent(Event aEvent)
        {
            _visibleEvents.Add(aEvent);
            VisibleEventAdded?.Invoke(this, new EventEventArgs(aEvent));
        }

        private void RemoveVisibleEvent(Event aEvent)
        {
            if (_visibleEvents.Remove(aEvent))
                VisibleEventRemoved?.Invoke(this, new EventEventArgs(aEvent));
        }

        #region PInvoke
        [DllImport("kernel32.dll")]
        private static extern int QueryUnbiasedInterruptTime(out ulong unbiasedTime);
        #endregion // static methods

    }

}