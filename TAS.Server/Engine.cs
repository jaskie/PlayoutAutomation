using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using TAS.Common;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.Security;
using TAS.Server.Media;
using TAS.Server.Security;
using TAS.Database.Common;
using jNet.RPC;

namespace TAS.Server
{
    public class Engine : jNet.RPC.Server.ServerObjectBase, IEngine, IEnginePersistent, IDisposable
    {

        private string _engineName;
        private bool _pst2Prv;

        [DtoMember(nameof(PlayoutChannelPRI))]
        private CasparServerChannel _playoutChannelPRI;

        [DtoMember(nameof(PlayoutChannelSEC))]
        private CasparServerChannel _playoutChannelSEC;

        [DtoMember(nameof(MediaManager))]
        private readonly MediaManager _mediaManager;

        [DtoMember(nameof(Preview))]
        private Preview _preview;

        [DtoMember(nameof(AuthenticationService))]
        private IAuthenticationService _authenticationService;

        Thread _engineThread;
        private long _currentTimeInTicks;
        public readonly object RundownSync = new object();

        private readonly List<Event> _visibleEvents = new List<Event>(); // list of visible events
        private readonly List<Event> _runningEvents = new List<Event>(); // list of events loaded and playing
        private IEvent _abortedEvent;
        private readonly ConcurrentDictionary<VideoLayer, IEvent> _preloadedEvents = new ConcurrentDictionary<VideoLayer, IEvent>();
        private readonly SynchronizedCollection<Event> _rootEvents = new SynchronizedCollection<Event>();
        private readonly SynchronizedCollection<Event> _fixedTimeEvents = new SynchronizedCollection<Event>();
        private readonly ConcurrentDictionary<ulong, Event> _events = new ConcurrentDictionary<ulong, Event>();
        private readonly Lazy<IList<IAclRight>> _rights;

        private Event _playing;
        private Event _forcedNext;
        private List<IGpi> _localGpis;
        private List<IEnginePlugin> _plugins;
        private int _timeCorrection;
        private bool _isWideScreen;
        private TEngineState _engineState;
        private double _programAudioVolume = 1;
        private bool _fieldOrderInverted;
        private EventRecorder _eventRecorder;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static TimeSpan _preloadTime = new TimeSpan(0, 0, 2); // time to preload event
        private bool _enableCGElementsForNewEvents;
        private bool _studioMode;
        private ConnectionStateRedundant _databaseConnectionState;
        private TVideoFormat _videoFormat;
        private bool _disposed;
        private bool _isAbortedRundown;

        public Engine()
        {
            _engineState = TEngineState.NotInitialized;
            _mediaManager = new MediaManager(this);
            _databaseConnectionState = DatabaseProvider.Database.ConnectionState;
            DatabaseProvider.Database.ConnectionStateChanged += _database_ConnectionStateChanged;
            _rights = new Lazy<IList<IAclRight>>(() =>
            {
                var rights = DatabaseProvider.Database.ReadEngineAclList<EngineAclRight>(this,
                        AuthenticationService as IAuthenticationServicePersitency);
                foreach (var r in rights)
                {
                    ((EngineAclRight)r).Saved += AclRight_Saved;
                }
                return rights;
            });
            FieldLengths = DatabaseProvider.Database.EngineFieldLengths;
            ServerMediaFieldLengths = DatabaseProvider.Database.ServerMediaFieldLengths;
            ArchiveMediaFieldLengths = DatabaseProvider.Database.ArchiveMediaFieldLengths;
            EventFieldLengths = DatabaseProvider.Database.EventFieldLengths;
        }

        public event EventHandler<EngineTickEventArgs> EngineTick;
        public event EventHandler<EngineOperationEventArgs> EngineOperation;
        public event EventHandler<EventEventArgs> VisibleEventAdded;
        public event EventHandler<EventEventArgs> VisibleEventRemoved;

        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        public event EventHandler<EventEventArgs> EventLocated;
        public event EventHandler<EventEventArgs> EventDeleted;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;


        #region IEngineProperties

        public ulong Instance { get; set; }

        public ulong IdArchive { get; set; }

        public ulong IdServerPRI { get; set; }

        public int ServerChannelPRI { get; set; }

        public ulong IdServerSEC { get; set; }

        public int ServerChannelSEC { get; set; }

        public ulong IdServerPRV { get; set; }

        public int ServerChannelPRV { get; set; }

        [DtoMember, Hibernate]
        public int CGStartDelay { get; set; }

        [DtoMember, Hibernate]
        public string EngineName { get => _engineName; set => SetField(ref _engineName, value); }

        [DtoMember, Hibernate]
        public bool EnableCGElementsForNewEvents { get => _enableCGElementsForNewEvents; set => SetField(ref _enableCGElementsForNewEvents, value); }

        [DtoMember, Hibernate]
        public bool StudioMode { get => _studioMode; set => SetField(ref _studioMode, value); }

        [DtoMember, Hibernate]
        public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }

        [Hibernate]
        public bool TryContinueRundownAfterEngineRestart { get; set; }

        #endregion //IEngineProperties

        public TArchivePolicyType ArchivePolicy { get; set; } = TArchivePolicyType.NoArchive;

        public IPreview Preview => _preview;

        public IMediaManager MediaManager => _mediaManager;

        [DtoMember]
        public ICGElementsController CGElementsController { get; private set; }

        [DtoMember]
        public IRouter Router { get; private set; }

        [Hibernate]
        public ServerHostWrapper Remote { get; set; }

        [DtoMember, Hibernate]
        public TAspectRatioControl AspectRatioControl { get; set; }

        [DtoMember, Hibernate]
        public int TimeCorrection
        {
            get => _timeCorrection;
            set => SetField(ref _timeCorrection, value);
        }

        public DateTime CurrentTime { get; private set; }

        public DateTime AlignDateTime(DateTime dt)
        {
            return new DateTime((dt.Ticks / FrameTicks) * FrameTicks, dt.Kind);
        }

        public TimeSpan AlignTimeSpan(TimeSpan ts)
        {
            return new TimeSpan((ts.Ticks / FrameTicks) * FrameTicks);
        }

        public IPlayoutServerChannel PlayoutChannelPRI => _playoutChannelPRI;

        public IPlayoutServerChannel PlayoutChannelSEC => _playoutChannelSEC;

        public long FrameTicks { get; private set; } = VideoFormatDescription.Descriptions[default].FrameTicks;

        public RationalNumber FrameRate => FormatDescription.FrameRate;

        public VideoFormatDescription FormatDescription { get; private set; } = VideoFormatDescription.Descriptions[default];

        [DtoMember, Hibernate]
        public TVideoFormat VideoFormat
        {
            get => _videoFormat; 
            set
            {
                if (!SetField(ref _videoFormat, value))
                    return;
                FormatDescription = VideoFormatDescription.Descriptions[value];
                FrameTicks = FormatDescription.FrameTicks;
            }
        }

        [DtoMember]
        public TEngineState EngineState
        {
            get => _engineState;
            private set
            {
                lock (RundownSync)
                    if (SetField(ref _engineState, value))
                    {
                        if (value == TEngineState.Hold)
                            foreach (var ev in _runningEvents.Where(e =>
                                (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) &&
                                e.IsFinished()).ToArray())
                                _pause(ev, true);
                        if (value == TEngineState.Idle && _runningEvents.Count > 0)
                        {
                            foreach (var ev in _runningEvents.Where(e =>
                                (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) &&
                                e.IsFinished()).ToArray())
                                _pause(ev, true);
                        }
                    }
            }
        }

        [DtoMember]
        public bool FieldOrderInverted
        {
            get => _fieldOrderInverted;
            set
            {
                if (!SetField(ref _fieldOrderInverted, value))
                    return;
                _playoutChannelPRI?.SetFieldOrderInverted(VideoLayer.Program, value);
                if (_playoutChannelSEC != null && !(_playoutChannelSEC == Preview.Channel && _preview?.IsMovieLoaded == true))
                    _playoutChannelSEC.SetFieldOrderInverted(VideoLayer.Program, value);
            }
        }

        [DtoMember]
        public double ProgramAudioVolume
        {
            get => _programAudioVolume;
            set
            {
                lock (RundownSync)
                    _setProgramAudioVolume(value, false);
            }
        }

        [DtoMember]
        public bool IsAbortedRundown => _abortedEvent != null;

        public void Initialize(IReadOnlyCollection<CasparServer> servers)
        {
            Logger.Debug("{0}: Initializing", EngineName);
            _authenticationService = Security.AuthenticationService.Current;
            var recorders = new List<CasparRecorder>();
            var sPRI = servers.FirstOrDefault(s => s.Id == IdServerPRI);
            _playoutChannelPRI = (CasparServerChannel)sPRI?.Channels.FirstOrDefault(c => c.Id == ServerChannelPRI);
            if (sPRI != null)
                recorders.AddRange(sPRI.Recorders.Select(r => r as CasparRecorder));
            var sSEC = servers.FirstOrDefault(s => s.Id == IdServerSEC);
            if (sSEC != null && sSEC != sPRI)
                recorders.AddRange(sSEC.Recorders.Select(r => r as CasparRecorder));
            _playoutChannelSEC = (CasparServerChannel)sSEC?.Channels.FirstOrDefault(c => c.Id == ServerChannelSEC);
            var sPRV = servers.FirstOrDefault(s => s.Id == IdServerPRV);
            if (sPRV != null && sPRV != sPRI && sPRV != sSEC)
                recorders.AddRange(sPRV.Recorders.Select(r => r as CasparRecorder));
            var previewChannel = sPRV?.Channels.FirstOrDefault(c => c.Id == ServerChannelPRV) as CasparServerChannel;
            if (previewChannel != null)
                _preview = new Preview(this, previewChannel);
            _mediaManager.SetupRecorders(recorders);
            _eventRecorder = new EventRecorder(this, servers);

            _localGpis = this.ComposeParts<IGpi>();
            _plugins = this.ComposeParts<IEnginePlugin>();
            CGElementsController = this.ComposePart<ICGElementsController>();
            Router = this.ComposePart<IRouter>();
            _isWideScreen = FormatDescription.IsWideScreen;
            var chPRI = PlayoutChannelPRI as CasparServerChannel;
            var chSEC = PlayoutChannelSEC as CasparServerChannel;
            if (chSEC != null && chSEC != chPRI)
            {
                chSEC.Owner.Initialize(_mediaManager);
                chSEC.Owner.PropertyChanged += _server_PropertyChanged;
            }
            if (chPRI != null)
            {
                chPRI.Owner.Initialize(_mediaManager);
                chPRI.Owner.PropertyChanged += _server_PropertyChanged;
            }

            DatabaseProvider.Database.ReadRootEvents(this);

            EngineState = TEngineState.Idle;
            if (CGElementsController != null)
            {
                CGElementsController.Started += _gpiStartLoaded;
            }

            Remote?.Initialize(this, new PrincipalProvider(_authenticationService));

            if (_localGpis != null)
                foreach (var gpi in _localGpis)
                    gpi.Started += _gpiStartLoaded;

            _engineThread = new Thread(ThreadProc)
            {
                Priority = ThreadPriority.Highest,
                Name = $"Engine main thread for {EngineName}",
                IsBackground = true
            };
            _engineThread.Start();
            Logger.Debug("{0}: Initialized", EngineName);
        }

        [DtoMember]
        public ConnectionStateRedundant DatabaseConnectionState { get => _databaseConnectionState; set => SetField(ref _databaseConnectionState, value); }

        public List<IEvent> FixedTimeEvents
        {
            get
            {
                lock (_fixedTimeEvents.SyncRoot) return _fixedTimeEvents.Cast<IEvent>().ToList();
            }
        }

        public ICollection<IEventPersistent> VisibleEvents
        {
            get
            {
                lock (_visibleEvents.SyncRoot())
                {
                    return _visibleEvents.Cast<IEventPersistent>().ToList();
                }
            }
        }

        /// <summary>
        /// Event playing on Program layer
        /// </summary>
        [DtoMember]
        public IEvent Playing
        {
            get => _playing;
        }

        [DtoMember]
        public IEvent NextToPlay
        {
            get
            {
                var e = _playing;
                if (e == null)
                    return null;
                lock (RundownSync)
                {
                    e = _successor(e);
                }
                if (e == null)
                    return null;
                if (e.EventType == TEventType.Rundown)
                    lock (RundownSync)
                    {
                        return e.FindVisibleSubEvent();
                    }
                return e;
            }
        }

        public IEvent GetNextWithRequestedStartTime()
        {
            lock (RundownSync)
            {
                var e = _playing;
                if (e == null)
                    return null;
                do
                    e = e.InternalGetSuccessor();
                while (e != null && e.RequestedStartTime == null);
                return e;
            }
        }

        [DtoMember]
        public IEvent ForcedNext
        {
            get => _forcedNext;
            private set
            {
                lock (RundownSync)
                {
                    var oldForcedNext = _forcedNext;
                    if (SetField(ref _forcedNext, (Event)value))
                    {
                        if (_forcedNext != null)
                            _forcedNext.IsForcedNext = true;
                        if (oldForcedNext != null)
                            oldForcedNext.IsForcedNext = false;
                        NotifyPropertyChanged(nameof(NextToPlay));
                    }
                }
            }
        }

        [DtoMember]
        public bool IsWideScreen
        {
            get { return _isWideScreen; }
            private set
            {
                if (SetField(ref _isWideScreen, value))
                    if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                    {
                        _playoutChannelPRI?.SetAspect(VideoLayer.Program, FormatDescription, !value);
                        _playoutChannelSEC?.SetAspect(VideoLayer.Program, FormatDescription, !value);
                    }
            }
        }

        [DtoMember]
        public bool Pst2Prv
        {
            get => _pst2Prv;
            set
            {
                if (!SetField(ref _pst2Prv, value))
                    return;
                if (value)
                    _loadPST();
                else
                    ((CasparServerChannel)Preview.Channel)?.Clear(VideoLayer.Preset);
            }
        }

        public void Load(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;
            if (aEvent == null || !(aEvent.EventType == TEventType.Rundown || aEvent.IsMovieOrLiveOnProgramLayer()))
                return;
            lock (RundownSync)
            {
                EngineState = TEngineState.Hold;
                List<Event> el;
                lock (_visibleEvents.SyncRoot())
                    el = _visibleEvents.ToList();
                foreach (var e in el)
                    _stop(e);
                _clearRunning();
                _load(aEvent as Event);
            }
        }

        public void StartLoaded()
        {
            if (!HaveRight(EngineRight.Play))
                return;

            _startLoaded();
        }

        public void Start(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;

            if (!(aEvent is Event ets))
                return;
            _start(ets);
        }

        public void Schedule(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;

            lock (RundownSync)
            {
                EngineState = TEngineState.Running;
                _run((Event)aEvent);
            }
            NotifyEngineOperation(aEvent, TEngineOperation.Schedule);
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Logger.Info("{0}: Clear layer {1}", EngineName, aVideoLayer);
            Event ev;
            lock (_visibleEvents.SyncRoot())
                ev = _visibleEvents.FirstOrDefault(e => e.Layer == aVideoLayer);
            lock (RundownSync)
            {
                if (ev != null)
                {
                    ev.PlayState = ev.Position == 0 ? TPlayState.Scheduled : TPlayState.Aborted;
                    SaveEventDelayed(ev);
                    RemoveVisibleEvent(ev);
                    _runningEvents.Remove(ev);
                    RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(ev, CollectionOperation.Remove));
                }
                _playoutChannelPRI?.Clear(aVideoLayer);
                _playoutChannelSEC?.Clear(aVideoLayer);
                if (aVideoLayer == VideoLayer.Program)
                    _setPlaying(null);
            }
        }

        public void Clear()
        {
            if (!HaveRight(EngineRight.Play))
                return;

            Logger.Info("{0}: Clear all", EngineName);
            _eventRecorder.EndCapture();
            _abortedEvent = Playing;
            lock (RundownSync)
            {
                _clear();
            }
            NotifyEngineOperation(null, TEngineOperation.Clear);
            NotifyPropertyChanged(nameof(IsAbortedRundown));
        }

        public void ClearMixer()
        {
            if (!HaveRight(EngineRight.Play))
                return;

            _playoutChannelPRI?.ClearMixer();
            _playoutChannelSEC?.ClearMixer();
        }

        public void RefreshVisibleEventsOnPlayer()
        {
            if (!HaveRight(EngineRight.Play))
                return;
            Logger.Info("{0}: Restart", EngineName);
            lock (RundownSync)
                _refreshVisibleEventsOnPlayer();
        }

        public bool ContinueAbortedRundown()
        {
            if (!HaveRight(EngineRight.Play))
                return false;
            var result = false;
            lock (RundownSync)
            {
                result = _continueAbortedRundown();
                _abortedEvent = null;
            }
            NotifyPropertyChanged(nameof(IsAbortedRundown));
            return result;
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
            if (media.IsProtected)
                return new MediaDeleteResult { Result = MediaDeleteResult.MediaDeleteResultEnum.Protected, Media = media };
            if (!(media is ServerMedia serverMedia))
                return reason;
            foreach (Event e in _rootEvents.ToList())
            {
                reason = e.CheckCanDeleteMedia(serverMedia);
                if (reason.Result != MediaDeleteResult.MediaDeleteResultEnum.Success)
                    return reason;
            }
            return DatabaseProvider.Database.MediaInUse(this, serverMedia);
        }

        public IReadOnlyCollection<IEvent> GetRootEvents() { lock (_rootEvents.SyncRoot) return _rootEvents.ToList<IEvent>(); }

        public void AddRootEvent(IEvent aEvent)
        {
            if (!(aEvent is Event ev))
                return;
            _rootEvents.Add(ev);
            NotifyEventLocated(ev);
        }

        internal bool RemoveRootEvent(Event aEvent)
        {
            return _rootEvents.Remove(aEvent);
        }

        public IEvent CreateNewEvent(
            ulong idRundownEvent = 0,
            ulong idEventBinding = 0,
            VideoLayer videoLayer = VideoLayer.None,
            TEventType eventType = TEventType.Rundown,
            TStartType startType = TStartType.None,
            TPlayState playState = TPlayState.Scheduled,
            DateTime scheduledTime = default,
            TimeSpan duration = default,
            TimeSpan scheduledDelay = default,
            TimeSpan scheduledTC = default,
            Guid mediaGuid = default,
            string eventName = "",
            DateTime startTime = default,
            TimeSpan startTC = default,
            TimeSpan? requestedStartTime = null,
            TimeSpan transitionTime = default,
            TimeSpan transitionPauseTime = default,
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
            int templateLayer = 10,
            short routerPort = -1,
            RecordingInfo recordingInfo = null
        )
        {
            if (idRundownEvent != 0
                && _events.TryGetValue(idRundownEvent, out var result))
                return result;
            switch (eventType)
            {
                case TEventType.Animation:
                    result = new AnimatedEvent(this, idRundownEvent, idEventBinding, videoLayer, startType, playState, scheduledTime, duration, scheduledDelay, mediaGuid, eventName, startTime, isEnabled, fields, method, templateLayer);
                    break;
                case TEventType.CommandScript:
                    result = new CommandScriptEvent(this, idRundownEvent, idEventBinding, startType, playState, scheduledDelay, eventName, startTime, isEnabled, command);
                    break;
                default:
                    result = new Event(this, idRundownEvent, idEventBinding, videoLayer, eventType, startType, playState, scheduledTime, duration, scheduledDelay, scheduledTC, mediaGuid, eventName, startTime, startTC, requestedStartTime, transitionTime, transitionPauseTime, transitionType, transitionEasing, audioVolume, idProgramme, idAux, isEnabled, isHold, isLoop, autoStartFlags, isCGEnabled, crawl, logo, parental, routerPort, recordingInfo);
                    break;
            }
            if (idRundownEvent != 0)
                _events.TryAdd(idRundownEvent, result);
            if (startType == TStartType.OnFixedTime)
                _fixedTimeEvents.Add(result);
            return result;
        }

        public void ReSchedule(IEvent aEvent)
        {
            if (!HaveRight(EngineRight.Play))
                return;
            Task.Run(() =>
            {
                try
                {
                    lock (RundownSync)
                    {
                        _reSchedule(aEvent as Event);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "{0}: ReSchedule exception", EngineName);
                }
            });
        }

        public void Execute(string command)
        {
            _playoutChannelPRI?.Execute(command);
            _playoutChannelSEC?.Execute(command);
        }

        public int CheckDatabase(bool recoverLostEvents)
        {
            if (!CurrentUser.IsAdmin)
                return 0;
            return DatabaseProvider.Database.CheckDatabase(this, recoverLostEvents);
        }

        #region  IPersistent properties
        public ulong Id { get; set; }

        public IAuthenticationService AuthenticationService => _authenticationService;

        [DtoMember]
        public IDictionary<string, int> FieldLengths { get; }

        [DtoMember]
        public IDictionary<string, int> ServerMediaFieldLengths { get; }

        [DtoMember]
        public IDictionary<string, int> ArchiveMediaFieldLengths { get; }

        [DtoMember]
        public IDictionary<string, int> EventFieldLengths { get; }

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
            lock (_rights) return _rights.Value.ToArray();
        }

        public IAclRight AddRightFor(ISecurityObject securityObject)
        {
            var right = new EngineAclRight { Owner = this, SecurityObject = securityObject };
            lock (_rights)
            {
                _rights.Value.Add(right);
            }
            right.Saved += AclRight_Saved;
            return right;
        }

        public void DeleteRight(IAclRight item)
        {
            var right = (AclRightBase)item;
            lock (_rights)
            {
                var success = _rights.Value.Remove(right);
                if (!success)
                    return;
                right.Delete();
                right.Saved -= AclRight_Saved;
            }
        }

        [DtoMember]
        public ulong CurrentUserRights
        {
            get
            {
                if (!(Thread.CurrentPrincipal.Identity is IUser user))
                    return 0UL;
                if (user.IsAdmin)
                {
                    var values = Enum.GetValues(typeof(EngineRight)).Cast<ulong>();
                    return values.Aggregate<ulong, ulong>(0, (current, value) => current | value);
                }
                var groups = user.GetGroups();
                lock (_rights)
                {
                    var userRights = _rights.Value.Where(r => r.SecurityObject == user);
                    var result = userRights.Aggregate(0UL, (current, right) => current | right.Acl);
                    var groupRights = _rights.Value.Where(r => groups.Any(g => g == r.SecurityObject));
                    result = groupRights.Aggregate(result, (current, groupRight) => current | groupRight.Acl);
                    return result;
                }
            }
        }

        #endregion // IAclObject

        public bool HaveRight(EngineRight right)
        {
            if (!(Thread.CurrentPrincipal.Identity is IUser user))
                return false;
            if (user.IsAdmin)
                return true; // Full rights
            var groups = user.GetGroups();
            lock (_rights)
            {
                return _rights.Value.Any(r => r.SecurityObject == user && (r.Acl & (ulong)right) > 0)
                    || groups.Any(g => _rights.Value.Any(r => r.SecurityObject == g && (r.Acl & (ulong)right) > 0));
            }
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

        internal void RemoveEvent(Event ev)
        {
            RemoveRootEvent(ev);
            if (ev.StartType == TStartType.OnFixedTime)
                RemoveFixedTimeEvent(ev);
            if (ev.Id != 0)
                _events.TryRemove(ev.Id, out _);

            if (ev.Media is ServerMedia media
                && ev.PlayState == TPlayState.Played
                && media.MediaType == TMediaType.Movie
                && ArchivePolicy == TArchivePolicyType.ArchivePlayedAndNotUsedWhenDeleteEvent
                && _mediaManager.ArchiveDirectory != null
                && CanDeleteMedia(media).Result == MediaDeleteResult.MediaDeleteResultEnum.Success)
                Task.Run(() =>
                    _mediaManager.MediaArchive(new List<IServerMedia>(new[] { media }), true, false));
        }

        internal void NotifyEventDeleted(Event @event)
        {
            EventDeleted?.Invoke(this, new EventEventArgs(@event));
        }


        // private methods
        private void _start(Event aEvent)
        {
            lock (RundownSync)
            {
                EngineState = TEngineState.Running;
                List<Event> eventsToStop;
                lock (_visibleEvents.SyncRoot())
                    eventsToStop = _visibleEvents.Where(e => e.PlayState == TPlayState.Played || e.PlayState == TPlayState.Playing).ToList();
                _clearRunning();
                _play(aEvent, true);
                foreach (var e in eventsToStop)
                    _stop(e);
            }
        }

        private void _reSchedule(Event aEvent)
        {
            if (aEvent == null)
                return;
                try
                {
                    if (aEvent.PlayState == TPlayState.Aborted
                        || aEvent.PlayState == TPlayState.Played)
                    {
                        aEvent.PlayState = TPlayState.Scheduled;
                        foreach (Event se in aEvent.GetSubEvents())
                            _reSchedule(se);
                    }

                    var next = (Event)aEvent.GetSuccessor();
                    if (next != null)
                        _reSchedule(next);
                }
                finally
                {
                    aEvent.Save();
                }
        }

        private void _load(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.InternalGetSuccessor();
            if (aEvent == null)
                return;
            Logger.Info("{0}: Load {1}", EngineName, aEvent);

            if (aEvent.EventType == TEventType.Live && Router?.SwitchOnPreload == true)
                Router.SelectInputPort(aEvent.RouterPort, true);

            if (aEvent.IsVisibleEvent())
            {
                _playoutChannelPRI?.Load(aEvent);
                _playoutChannelSEC?.Load(aEvent);
                SetVisibleEvent(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                    _setPlaying(aEvent);
            }
            _run(aEvent);
            aEvent.PlayState = TPlayState.Paused;
            NotifyEngineOperation(aEvent, TEngineOperation.Load);
            foreach (Event se in (aEvent.GetSubEvents().Where(e => e.ScheduledDelay == TimeSpan.Zero)))
                _load(se);
        }

        private void _loadNext(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.InternalGetSuccessor();
            if (aEvent == null)
                return;

            if ((aEvent.IsVisibleEvent()) &&
                !(_preloadedEvents.TryGetValue(aEvent.Layer, out var preloaded) && preloaded == aEvent))
            {
                Logger.Info("{0}: Preload {1}", EngineName, aEvent);
                _preloadedEvents[aEvent.Layer] = aEvent;
                _playoutChannelPRI?.LoadNext(aEvent);
                _playoutChannelSEC?.LoadNext(aEvent);

                if (aEvent.EventType == TEventType.Live && Router?.SwitchOnPreload == true)
                    Router.SelectInputPort(aEvent.RouterPort, true);

                if (!aEvent.IsHold
                    && CGElementsController != null
                    && CGStartDelay < 0)
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(_preloadTime + TimeSpan.FromMilliseconds(CGStartDelay));
                        try
                        {
                            CGElementsController.SetState(aEvent);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "{0}: Error setting CG state", EngineName);
                        }
                    });
                }
            }
            _run(aEvent);
        }

        private void _play(Event aEvent, bool fromBeginning)
        {
            if (aEvent == null)
                return;

            if (!aEvent.IsEnabled || (aEvent.Length == TimeSpan.Zero && !aEvent.IsAnimationOrCommandScript()))
                aEvent = aEvent.InternalGetSuccessor();
            Logger.Info("{0}: Play {1}", EngineName, aEvent);
            if (aEvent == _forcedNext)
            {
                ForcedNext = null;
                _runningEvents.ToList().ForEach(
                    e =>
                    {
                        if (e.PlayState == TPlayState.Playing)
                        {
                            e.PlayState = e.IsFinished() ? TPlayState.Played : TPlayState.Aborted;
                            _runningEvents.Remove(e);
                            RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, CollectionOperation.Remove));
                        }
                        SaveEventDelayed(e);
                    });
            }
            _run(aEvent);
            if (fromBeginning)
                aEvent.Position = 0;
            if (aEvent.IsVisibleEvent())
            {
                _eventRecorder.EndCapture();

                if (aEvent.RecordingInfo != null)
                    _eventRecorder.StartCapture(aEvent);

                if (aEvent.EventType == TEventType.Live && Router?.SwitchOnPreload == false)
                    Router.SelectInputPort(aEvent.RouterPort, false);

                _playoutChannelPRI?.Play(aEvent);
                _playoutChannelSEC?.Play(aEvent);
                SetVisibleEvent(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                {
                    _setPlaying(aEvent);
                    _setProgramAudioVolume(aEvent.GetAudioVolumeLinearValue(), true);
                    _setAspectRatio(aEvent);
                    if (CGElementsController != null)
                    {
                        if (CGStartDelay <= 0)
                            CGElementsController.SetState(aEvent);
                        else
                        {
                            Task.Run(() =>
                            {
                                Thread.Sleep(CGStartDelay);
                                CGElementsController.SetState(aEvent);
                            });
                        }
                    }
                }
                _preloadedEvents.TryRemove(aEvent.Layer, out _);
            }
            if (aEvent.IsAnimationOrCommandScript())
            {
                _playoutChannelPRI?.Play(aEvent);
                _playoutChannelSEC?.Play(aEvent);
                aEvent.PlayState = TPlayState.Played;
            }
            else
            {
                aEvent.PlayState = TPlayState.Playing;
                if (aEvent.SubEventsCount > 0)
                {
                    foreach (Event se in aEvent.GetSubEvents())
                        if (aEvent.OccupiesSameVideoLayerAs(se))
                        {
                            Logger.Error("{0}: Tried to play {1} on the same layer as parent {2}. Play ignored.", EngineName, se, aEvent);
                        }
                        else
                        {
                            if (se.ScheduledDelay == TimeSpan.Zero && (aEvent.EventType == TEventType.Rundown || se.Layer != aEvent.Layer || se.IsAnimationOrCommandScript()))
                                _play(se, fromBeginning);
                        }
                }
            }
            SaveEventDelayed(aEvent);
            if (_pst2Prv)
                _loadPST();
            NotifyEngineOperation(aEvent, TEngineOperation.Play);
            if (aEvent.IsMovieOrLiveOnProgramLayer())
                Task.Run(() => DatabaseProvider.Database.AsRunLogWrite(Id, aEvent));
        }

        private void _startLoaded()
        {
            lock (RundownSync)
                if (EngineState == TEngineState.Hold)
                {
                    _visibleEvents.Where(e => e.PlayState == TPlayState.Played).ToList().ForEach(e => _stop(e));
                    foreach (var e in _runningEvents.ToArray())
                    {
                        if (!(e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading))
                            _play(e, false);
                    }
                    EngineState = TEngineState.Running;
                }
        }

        private void _clearRunning()
        {
            var runningEvents = _runningEvents.ToArray();
            foreach (var e in runningEvents)
            {
                e.PlayState = e.Position == 0 ? TPlayState.Scheduled : TPlayState.Aborted;
                RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, CollectionOperation.Remove));
                SaveEventDelayed(e);
            }
            _runningEvents.Clear();
        }

        private void _setAspectRatio(Event aEvent)
        {
            if (aEvent == null || !(aEvent.Layer == VideoLayer.Program || aEvent.Layer == VideoLayer.Preset))
                return;
            var media = aEvent.Media;
            var narrow = media != null && (!media.VideoFormat.IsWideScreen());
            IsWideScreen = !narrow;
            if (AspectRatioControl != TAspectRatioControl.GPI &&
                AspectRatioControl != TAspectRatioControl.GPIandImageResize)
                return;
            if (_localGpis != null)
                foreach (var gpi in _localGpis)
                    gpi.IsWideScreen = !narrow;
        }

        private void _run(Event aEvent)
        {
            if (aEvent.IsAnimationOrCommandScript() || _runningEvents.Contains(aEvent))
                return;
            _runningEvents.Add(aEvent);
            RunningEventsOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(aEvent, CollectionOperation.Add));
        }

        private void _stop(Event aEvent)
        {
            aEvent.PlayState = aEvent.Position == 0 ? TPlayState.Scheduled : aEvent.IsFinished() ? TPlayState.Played : TPlayState.Aborted;
            SaveEventDelayed(aEvent);
            lock (_visibleEvents.SyncRoot())
                if (_visibleEvents.Contains(aEvent))
                {
                    var eventType = aEvent.EventType;
                    if (eventType != TEventType.Live && eventType != TEventType.CommandScript)
                    {
                        Logger.Info("{0}: Stop {1}", EngineName, aEvent);
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
            lock (_visibleEvents.SyncRoot())
                if (_visibleEvents.Contains(aEvent))
                {
                    Logger.Info("{0}: Pause {1}", EngineName, aEvent);
                    if (aEvent.EventType == TEventType.Movie)
                    {
                        _playoutChannelPRI?.Pause(aEvent);
                        _playoutChannelSEC?.Pause(aEvent);
                    }
                    foreach (Event se in aEvent.GetSubEvents())
                        _pause(se, finish);
                }
            if (finish)
            {
                aEvent.PlayState = TPlayState.Played;
                SaveEventDelayed(aEvent);
                _runningEvents.Remove(aEvent);
                NotifyEngineOperation(aEvent, TEngineOperation.Stop);
            }
            else
                NotifyEngineOperation(aEvent, TEngineOperation.Pause);
        }

        private void _loadPST()
        {
            if (!(Preview.Channel is CasparServerChannel channel))
                return;
            if (NextToPlay is Event ev)
            {
                MediaBase media = ev.ServerMediaPRV;
                if (media != null)
                {
                    channel.Load(media, VideoLayer.Preset, 0, -1);
                    return;
                }
            }
            channel.Load(System.Drawing.Color.Black, VideoLayer.Preset);
        }

        private void _refreshVisibleEventOnPlayer(Event ev, bool start)
        {
            if (ev == null)
                return;
            _setProgramAudioVolume(ev.GetAudioVolumeLinearValue(), false);
            _playoutChannelPRI?.RefreshPlayback(ev, start);
            _playoutChannelSEC?.RefreshPlayback(ev, start);
            if (CGElementsController != null)
            {
                CGElementsController.Clear();
                CGElementsController.SetState(ev);
            }
        }

        private bool _continueAbortedRundown()
        {
            if (_abortedEvent is null || EngineState == TEngineState.Running)
                return false;
            if (!(_abortedEvent is Event currentEvent && currentEvent.IsMovieOrLiveOnProgramLayer())) // currentEvent contains previously playing movie or live
                return false;
            var startTime = currentEvent.StartTime;
            var currentTime = CurrentTime;
            if (startTime == default || startTime > currentTime)
                return false;
            Event baseEvent = null;
            // step 1: find the base event that should be running now on Program layer
            while (currentEvent != null)
            {
                if (startTime < currentTime && startTime + currentEvent.Length > currentTime) // we have the event that should be running now
                {
                    baseEvent = currentEvent;
                    break;
                }
                else
                {
                    startTime += currentEvent.Length;
                    currentEvent = currentEvent.InternalGetSuccessor();
                }
            }
            // step 2: if base event is found, run it and its relatives
            if (baseEvent != null)
            {
                // step 2.1: clear engine and start the event itself
                _clear();
                _setPlaying(baseEvent);
                baseEvent.InternalSetPlaying(startTime, (currentTime.Ticks - startTime.Ticks) / FrameTicks);
                _run(baseEvent);
                SetVisibleEvent(baseEvent);
                _refreshVisibleEventOnPlayer(baseEvent, true);
                if (CGElementsController != null)
                {
                    CGElementsController.Clear();
                    CGElementsController.SetState(baseEvent); // call the controler, ignoring its delay for sake of simplicity
                }
                // step 2.2: start its subevents that should be running
                foreach (var se in baseEvent.GetSubEvents().Where(e => e.IsMovieOrStill()).Cast<Event>())
                {
                    DateTime seStartTime = default;
                    switch (se.StartType)
                    {
                        case TStartType.WithParent:
                            seStartTime = startTime + se.ScheduledDelay;
                            break;
                        case TStartType.WithParentFromEnd:
                            seStartTime = startTime + baseEvent.Duration - se.Duration - se.ScheduledDelay;
                            break;
                        default:
                            continue;
                    }
                    if (seStartTime > currentTime || seStartTime + se.Length < currentTime) // not started yet or already finished
                        continue;
                    se.InternalSetPlaying(seStartTime, (currentTime.Ticks - seStartTime.Ticks) / FrameTicks);
                    _run(se);
                    SetVisibleEvent(se);
                    _refreshVisibleEventOnPlayer(se, true);
                }
                Logger.Info("{0}: ContinueAbortedRundown executed for {1} starting {2}", EngineName, _abortedEvent, baseEvent);

                // step 2.3 (TODO in future, if needed) start its owners, if should be running

                // step 3: run the engine
                SetField(ref _engineState, TEngineState.Running, nameof(EngineState));
                return true;
            }
            return false;
        }

        private void _tick(long nFrames)
        {
            lock (RundownSync)
            {
                if (EngineState == TEngineState.Running)
                {
                    foreach (var e in _runningEvents.Where(ev => ev.PlayState == TPlayState.Playing || ev.PlayState == TPlayState.Fading))
                        e.Position += nFrames;

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
                            if (playingEvent.Position >= playingEvent.LengthInFrames - succEvent.TransitionInFrames())
                            {
                                if (succEvent.IsHold && succEvent != _forcedNext)
                                    EngineState = TEngineState.Hold;
                                else
                                    _play(succEvent, true);
                            }
                        }

                        // preload and play subevents, which was not started immediately with parent
                        playingEvent = _playing; // in case when succEvent just started 
                        if (playingEvent != null && playingEvent.SubEventsCount > 0)
                        {
                            TimeSpan playingEventPosition = TimeSpan.FromTicks(playingEvent.Position * FrameTicks);
                            TimeSpan playingEventDuration = playingEvent.Duration;
                            foreach (var se in playingEvent.GetSubEvents().Cast<Event>().Where(e =>
                                    e.PlayState == TPlayState.Scheduled &&
                                    !playingEvent.OccupiesSameVideoLayerAs(e) // we can't log this errorneous situation, as it would flood the log, so just ignore it and not process such items
                                    ))
                            {
                                IEvent preloaded;
                                switch (se.StartType)
                                {
                                    case TStartType.WithParent:
                                        if (se.IsMovieOrStill()
                                            && playingEventPosition >= se.ScheduledDelay - _preloadTime - se.TransitionTime
                                            && !(_preloadedEvents.TryGetValue(se.Layer, out preloaded) && se == preloaded))
                                            _loadNext(se);
                                        if (playingEventPosition >= se.ScheduledDelay - se.TransitionTime)
                                            _play(se, true);
                                        break;
                                    case TStartType.WithParentFromEnd:
                                        if (se.IsMovieOrStill()
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
                    {
                        _eventRecorder.EndCapture();
                        EngineState = TEngineState.Idle;
                    }
                }

                _executeAutoStartEvents();
            }
        }

        private void _executeAutoStartEvents()
        {
            var currentTimeOfDayTicks = CurrentTime.TimeOfDay.Ticks;
            lock (_fixedTimeEvents.SyncRoot)
            {
                var startEvent = _fixedTimeEvents
                    .FirstOrDefault(e =>
                                    e.StartType == TStartType.OnFixedTime &&
                                    (EngineState == TEngineState.Idle || (e.AutoStartFlags & AutoStartFlags.Force) == AutoStartFlags.Force) &&
                                    (e.PlayState == TPlayState.Scheduled || (e.PlayState != TPlayState.Playing && (e.AutoStartFlags & AutoStartFlags.Force) == AutoStartFlags.Force)) &&
                                    e.IsEnabled &&
                                    ((e.AutoStartFlags & AutoStartFlags.Daily) == AutoStartFlags.Daily ?
                                       currentTimeOfDayTicks >= e.ScheduledTime.TimeOfDay.Ticks && currentTimeOfDayTicks < e.ScheduledTime.TimeOfDay.Ticks + TimeSpan.TicksPerSecond :
                                       _currentTimeInTicks >= e.ScheduledTime.Ticks && _currentTimeInTicks < e.ScheduledTime.Ticks + TimeSpan.TicksPerSecond) // auto start only within 1 second slot
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
            result = (playingEvent.IsLoop ? playingEvent : playingEvent.InternalGetSuccessor()) ?? playingEvent.GetVisualRootTrack().FirstOrDefault(e => e.IsLoop) as Event;
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
                lock (RundownSync)
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
            pe = (Event)pe.GetSuccessor();
            while (pe != null)
            {
                var pauseTime = pe.GetAttentionTime();
                if (pauseTime != null)
                    return result + pauseTime.Value - pe.TransitionTime;
                result = result + pe.Length - pe.TransitionTime;
                pe = (Event)pe.GetSuccessor();
            }
            return result;
        }

        private void _database_ConnectionStateChanged(object sender, RedundantConnectionStateEventArgs e)
        {
            DatabaseConnectionState = e.NewState;
            Logger.Error("Database state changed from {0} to {1}. Stack trace was {2}", e.OldState, e.NewState, new StackTrace());
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (var e in _rootEvents)
                e.SaveLoadedTree();
            lock (_rights)
            {
                if (_rights.IsValueCreated)
                    foreach (var r in _rights.Value)
                        ((EngineAclRight)r).Saved -= AclRight_Saved;
            }
            DatabaseProvider.Database.ConnectionStateChanged -= _database_ConnectionStateChanged;
            (CGElementsController as IDisposable)?.Dispose();
            (Router as IDisposable)?.Dispose();
            Remote?.Dispose();
            _preview?.Dispose();
            _mediaManager.Dispose();
            if (_plugins != null)
                foreach (var plugin in _plugins)
                    (plugin as IDisposable)?.Dispose();
            Logger.Info("{0}: Engine disposed", EngineName);
        }

        private void NotifyEngineOperation(IEvent aEvent, TEngineOperation operation)
        {
            EngineOperation?.Invoke(this, new EngineOperationEventArgs(aEvent, operation));
        }


        private void ThreadProc()
        {
            Logger.Debug("{0}: Started engine thread", EngineName);
            CurrentTime = AlignDateTime(DateTime.UtcNow + TimeSpan.FromMilliseconds(_timeCorrection));
            _currentTimeInTicks = CurrentTime.Ticks;

            var playingEvents = DatabaseProvider.Database.SearchPlaying(this).Cast<Event>().ToList();
            var playing = playingEvents.FirstOrDefault(e => e.IsMovieOrLiveOnProgramLayer());
            if (playing != null)
            {
                // the event didn't finished yet - we do not execute it, but mark as running, assuming that it's still playing on playout channel(s)
                if (_currentTimeInTicks < playing.StartTime.Ticks + playing.Duration.Ticks) 
                {
                    foreach (var e in playingEvents)
                    {
                        e.Position = (_currentTimeInTicks - e.StartTime.Ticks) / FrameTicks;
                        _run(e);
                        if (e.IsVisibleEvent())
                            SetVisibleEvent(e);
                    }
                    _engineState = TEngineState.Running;
                    _setPlaying(playing);
                }
                else
                {
                    playingEvents.ForEach(e => e.AbortAndSave());
                    Logger.Debug("{0}: Found aborted event: {1}", EngineName, playing);
                    _abortedEvent = playing;
                    if (TryContinueRundownAfterEngineRestart)
                        _continueAbortedRundown();
                    else
                        NotifyPropertyChanged(nameof(IsAbortedRundown));
                }
            }
            else
                playingEvents.ForEach(e => e.AbortAndSave());

            var frameDuration = (ulong)FrameTicks;
            QueryUnbiasedInterruptTime(out var unbiasedTime);
            var prevTime = unbiasedTime - frameDuration;
            while (!_disposed)
            {
                try
                {
                    CurrentTime = AlignDateTime(DateTime.UtcNow + TimeSpan.FromMilliseconds(_timeCorrection));
                    QueryUnbiasedInterruptTime(out unbiasedTime);
                    _currentTimeInTicks = CurrentTime.Ticks;
                    var nFrames = (unbiasedTime - prevTime) / frameDuration;
                    prevTime += nFrames * frameDuration;
                    _tick((long)nFrames);
                    _preview?.Tick(_currentTimeInTicks, (long)nFrames);
                    EngineTick?.Invoke(this, new EngineTickEventArgs(CurrentTime, _getTimeToAttention()));
                    if (nFrames > 1)
                    {
                        if (nFrames > 20)
                            Logger.Warn("{0} LateFrame: {1}", EngineName, nFrames);
                        else
                            Logger.Debug("{0} LateFrame: {1}", EngineName, nFrames);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "{0}: Exception in Engine tick", EngineName);
                }
                QueryUnbiasedInterruptTime(out unbiasedTime);
                var waitTime = (int)((prevTime + frameDuration - unbiasedTime + 10000) / 10000);
                if (waitTime > 0)
                    Thread.Sleep(waitTime);
#if DEBUG
                else
                    Debug.WriteLineIf(waitTime < 0, "Negative waitTime");
#endif
            }
            Logger.Debug("{0}: Thread finished", EngineName);
        }

        private void _gpiStartLoaded(object o, EventArgs e)
        {
            _startLoaded();
        }

        private void _server_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            void ChannelConnected(CasparServerChannel channel, List<Event> ve)
            {
                foreach (Event ev in ve)
                {
                    channel.RefreshPlayback(ev, EngineState == TEngineState.Running);
                    channel.SetVolume(VideoLayer.Program, _programAudioVolume, 0);
                    if (ev.Layer == VideoLayer.Program || ev.Layer == VideoLayer.Preset)
                    {
                        IMedia media = ev.Media;
                        var narrow = media != null && (!media.VideoFormat.IsWideScreen());
                        if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                            channel.SetAspect(VideoLayer.Program, FormatDescription, narrow);
                    }
                }
            }

            if (e.PropertyName == nameof(IPlayoutServer.IsConnected) && ((IPlayoutServer)sender).IsConnected)
            {
                List<Event> ve;
                lock (_visibleEvents.SyncRoot())
                    ve = _visibleEvents.ToList();
                if (sender == ((CasparServerChannel)PlayoutChannelPRI)?.Owner)
                    ChannelConnected(_playoutChannelPRI, ve);
                if (sender == ((CasparServerChannel)PlayoutChannelSEC)?.Owner
                    && PlayoutChannelSEC != PlayoutChannelPRI)
                    ChannelConnected(_playoutChannelSEC, ve);
            }
        }

        internal void NotifyMediaVerified(MediaEventArgs ea)
        {
            _events.Where(e => e.Value.MediaGuid == ea.Media.MediaGuid)
                .ToList()
                .ForEach(e => e.Value.NotifyMediaVerified(ea.Media));
        }

        internal void NotifyEventLocated(Event aEvent)
        {
            EventLocated?.Invoke(this, new EventEventArgs(aEvent));
        }

        private void _setPlaying(Event aEvent)
        {
            Debug.Assert(aEvent is null || aEvent.Layer == VideoLayer.Program);
            var oldPlaying = _playing;
            if (!SetField(ref _playing, aEvent, nameof(Playing)))
                return;
            if (oldPlaying != null)
                oldPlaying.SubEventChanged -= _playingSubEventsChanged;
            if (aEvent != null)
            {
                aEvent.SubEventChanged += _playingSubEventsChanged;
                var media = aEvent.Media;
                SetField(ref _fieldOrderInverted, media?.FieldOrderInverted ?? false, nameof(FieldOrderInverted));
            }
            NotifyPropertyChanged(nameof(NextToPlay));
        }

        private void _setProgramAudioVolume(double value, bool useTransition)
        {
            if (!SetField(ref _programAudioVolume, value, nameof(ProgramAudioVolume)))
                return;
            var playing = _playing;
            int transitioDuration = (!useTransition || playing is null) ? 0 : (int)playing.TransitionTime.ToSmpteFrames(FrameRate);
            _playoutChannelPRI?.SetVolume(VideoLayer.Program, value, transitioDuration);
            if (_playoutChannelSEC != null && !(_playoutChannelSEC == Preview.Channel && _preview?.IsMovieLoaded == true))
                _playoutChannelSEC.SetVolume(VideoLayer.Program, value, transitioDuration);
        }

        private void _refreshVisibleEventsOnPlayer()
        {
            List<Event> le;
            lock (_visibleEvents.SyncRoot())
                le = _visibleEvents.ToList();
            foreach (var e in le)
                _refreshVisibleEventOnPlayer(e, _engineState == TEngineState.Running);
        }

        private void _clear()
        {
            _clearRunning();
            lock (_visibleEvents.SyncRoot())
                _visibleEvents.Clear();
            ForcedNext = null;
            _playoutChannelPRI?.Clear();
            _playoutChannelSEC?.Clear();
            _setProgramAudioVolume(1, false);
            EngineState = TEngineState.Idle;
            _setPlaying(null);
            CGElementsController?.Clear();
        }

        private void SetVisibleEvent(Event aEvent)
        {
            lock (_visibleEvents.SyncRoot())
            {
                var oldEvent = _visibleEvents.Find(e => e.Layer == aEvent.Layer);
                if (aEvent == oldEvent)
                    return;
                if (oldEvent == null)
                {
                    _visibleEvents.Add(aEvent);
                    VisibleEventAdded?.Invoke(this, new EventEventArgs(aEvent));
                }
                else
                {
                    _visibleEvents[_visibleEvents.IndexOf(oldEvent)] = aEvent;
                    VisibleEventRemoved?.Invoke(this, new EventEventArgs(oldEvent));
                    VisibleEventAdded?.Invoke(this, new EventEventArgs(aEvent));
                }
            }
        }

        private void RemoveVisibleEvent(Event aEvent)
        {
            lock (_visibleEvents.SyncRoot())
                if (_visibleEvents.Remove(aEvent))
                    VisibleEventRemoved?.Invoke(this, new EventEventArgs(aEvent));
        }

        private void AclRight_Saved(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(CurrentUserRights));
        }

        private void SaveEventDelayed(Event e)
        {
            Task.Run(() => e.Save());
        }

        #region PInvoke
        [DllImport("kernel32.dll")]
        private static extern int QueryUnbiasedInterruptTime(out ulong unbiasedTime);
        #endregion // static PInvoke

    }

}