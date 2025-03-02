using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Remoting.Model
{
    public class Engine : ProxyObjectBase, IEngine
    {
        #pragma warning disable CS0649, IDE0044

        [DtoMember(nameof(IEngine.CurrentTime))]
        private DateTime _currentTime;

        [DtoMember(nameof(IEngine.TimeCorrection))]
        private int _timeCorrection;

        [DtoMember(nameof(IEngine.EngineName))]
        private string _engineName;

        [DtoMember(nameof(IEngine.EngineState))]
        private TEngineState _engineState;

        [DtoMember(nameof(IEngine.ForcedNext))]
        private IEvent _forcedNext;

        [DtoMember(nameof(IEngine.CGElementsController))]
        private ICGElementsController _cGElementsController;

        [DtoMember(nameof(IEngine.Router))]
        private IRouter _router;

        [DtoMember(nameof(IEngine.EnableCGElementsForNewEvents))]
        private bool _enableCGElementsForNewEvents;

        [DtoMember(nameof(IEngine.StudioMode))]
        private bool _studioMode;

        [DtoMember(nameof(IEngine.CrawlEnableBehavior))]
        private TCrawlEnableBehavior _crawlEnableBehavior;

        [DtoMember(nameof(IEngine.FieldOrderInverted))]
        private bool _fieldOrderInverted;

        [DtoMember(nameof(IEngine.Preview))]
        private IPreview _preview;

        [DtoMember(nameof(IEngine.MediaManager))]
        private IMediaManager _mediaManager;

        [DtoMember(nameof(IEngine.PlayoutChannelPRI))]
        private IPlayoutServerChannel _playoutChannelPRI;

        [DtoMember(nameof(IEngine.PlayoutChannelSEC))]
        private IPlayoutServerChannel _playoutChannelSEC;

        [DtoMember(nameof(IEngine.IsWideScreen))]
        private bool _isWideScreen;

        [DtoMember(nameof(IEngine.ProgramAudioVolume))]
        private double _programAudioVolume;

        [DtoMember(nameof(IEngine.Pst2Prv))]
        private bool _pst2Prv;

        [DtoMember(nameof(IEngine.AuthenticationService))]
        private IAuthenticationService _authenticationService;

        [DtoMember(nameof(IEngine.VideoFormat))]
        private TVideoFormat _videoFormat;

        [DtoMember(nameof(IEngine.DatabaseConnectionState))]
        private ConnectionStateRedundant _databaseConnectionState;

        [DtoMember(nameof(IEngine.Playing))]
        private IEvent _playing;

        [DtoMember(nameof(IEngine.ServerMediaFieldLengths))]
        private IDictionary<string, int> _serverMediaFieldLengths;

        [DtoMember(nameof(IEngine.ArchiveMediaFieldLengths))]
        private IDictionary<string, int> _archiveMediaFieldLengths;

        [DtoMember(nameof(IEngine.EventFieldLengths))]
        private IDictionary<string, int> _eventFieldLengths;

        [DtoMember(nameof(IEngine.NextToPlay))]
        private IEvent _nextToPlay;

        #pragma warning restore

        public DateTime CurrentTime => _currentTime;

        public int TimeCorrection { get => _timeCorrection; set => Set(value); }

        public string EngineName => _engineName;

        public TEngineState EngineState => _engineState;

        public IEvent ForcedNext => _forcedNext;

        public VideoFormatDescription FormatDescription => VideoFormatDescription.Descriptions[VideoFormat];

        public RationalNumber FrameRate => FormatDescription.FrameRate;

        public long FrameTicks => FormatDescription.FrameTicks;

        public ICGElementsController CGElementsController => _cGElementsController;

        public IRouter Router => _router;

        public bool EnableCGElementsForNewEvents
        {
            get => _enableCGElementsForNewEvents;
            set => Set(value);
        }

        public bool StudioMode { get => _studioMode; set => Set(value); }

        public TCrawlEnableBehavior CrawlEnableBehavior
        {
            get => _crawlEnableBehavior;
            set => Set(value);
        }

        public bool FieldOrderInverted { get => _fieldOrderInverted; set => Set(value); }

        public IMediaManager MediaManager => _mediaManager;

        public IPreview Preview => _preview;

        public IPlayoutServerChannel PlayoutChannelPRI => _playoutChannelPRI;

        public IPlayoutServerChannel PlayoutChannelSEC => _playoutChannelSEC;

        public bool IsWideScreen => _isWideScreen;

        public IEvent NextToPlay { get => _nextToPlay; set => Set(value); }

        public IEvent GetNextWithRequestedStartTime() { return Query<IEvent>(); }



        public double ProgramAudioVolume { get => _programAudioVolume; set => Set(value); }

        public bool Pst2Prv { get => _pst2Prv; set => Set(value); }

        public IAuthenticationService AuthenticationService => _authenticationService;

        public IReadOnlyCollection<IEvent> GetRootEvents() { return Query<List<IEvent>>(); }

        public TVideoFormat VideoFormat { get => _videoFormat; set => Set(value); }

        public ConnectionStateRedundant DatabaseConnectionState => _databaseConnectionState;

        public IEvent Playing => _playing;

        public List<IEvent> FixedTimeEvents => throw new NotImplementedException();

        public IDictionary<string, int> ServerMediaFieldLengths { get => _serverMediaFieldLengths; set => Set(value); }
        public IDictionary<string, int> ArchiveMediaFieldLengths { get => _archiveMediaFieldLengths; set => Set(value); }
        public IDictionary<string, int> EventFieldLengths { get => _eventFieldLengths; set => Set(value); }

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
                    int templateLayer = -1,
                    short routerPort = -1,
                    RecordingInfo recordingInfo = null
            )
        {
            return Query<IEvent>(parameters: new object[] { idRundownEvent, idEventBinding , videoLayer, eventType, startType, playState, scheduledTime, duration, scheduledDelay, scheduledTC, mediaGuid, eventName,
                    startTime, startTC, requestedStartTime, transitionTime, transitionPauseTime, transitionType, transitionEasing, audioVolume, idProgramme, idAux, isEnabled, isHold, isLoop, isCGEnabled,
                    crawl, logo, parental, autoStartFlags, command, fields, method, templateLayer, routerPort, recordingInfo});
        }

        public void AddRootEvent(IEvent ev)
        {
            Invoke(parameters: new object[] { ev });
        }

        public void Clear() { Invoke(); }

        public void Clear(VideoLayer aVideoLayer) { Invoke(parameters: new object[] { aVideoLayer }); }

        public void ClearMixer() { Invoke(); }

        public void Load(IEvent aEvent) { Invoke(parameters: new object[] { aEvent }); }

        public void ReSchedule(IEvent aEvent) { Invoke(parameters: new object[] { aEvent }); }

        public void Restart() { Invoke(); }

        public void RestartRundown(IEvent aRundown) { Invoke(parameters: new object[] { aRundown }); }

        public void Schedule(IEvent aEvent) { Invoke(parameters: new object[] { aEvent }); }

        public int CheckDatabase(bool recoverLostEvents)
        {
            Invoke(parameters: new object[] { recoverLostEvents });
        }

        public void Start(IEvent aEvent) { Invoke(parameters: new object[] { aEvent }); }

        public void StartLoaded() { Invoke(); }

        public void ForceNext(IEvent aEvent) { Invoke(parameters: new object[] { aEvent }); }

        public void Execute(string command)
        {
            throw new NotImplementedException(); // method used by server plugin only
        }

        public IEnumerable<IAclRight> GetRights() => Query<IAclRight[]>();

        public IAclRight AddRightFor(ISecurityObject securityObject) { return Query<IAclRight>(parameters: new object[] { securityObject }); }

        public void DeleteRight(IAclRight item) { Invoke(parameters: new object[] { item }); }

        [DtoMember]
        public ulong CurrentUserRights { get; set; }

        [DtoMember]
        public TAspectRatioControl AspectRatioControl { get; set; }

        [DtoMember]
        public int CGStartDelay { get; set; }

        public bool HaveRight(EngineRight right)
        {
            return (CurrentUserRights & (ulong) right) > 0;
        }

        #region Event handling
        event EventHandler<EngineOperationEventArgs> _engineOperation;
        public event EventHandler<EngineOperationEventArgs> EngineOperation
        {
            add
            {
                EventAdd(_engineOperation);
                _engineOperation += value;
            }
            remove
            {
                _engineOperation -= value;
                EventRemove(_engineOperation);
            }
        }
        event EventHandler<EngineTickEventArgs> _engineTick;
        public event EventHandler<EngineTickEventArgs> EngineTick
        {
            add
            {
#if !DEBUG
                EventAdd(_engineTick);
#endif
                _engineTick += value;
            }
            remove
            {
                _engineTick -= value;
#if !DEBUG
                EventRemove(_engineTick);
#endif
            }
        }
        event EventHandler<EventEventArgs> _eventLocated;
        public event EventHandler<EventEventArgs> EventLocated
        {
            add
            {
                EventAdd(_eventLocated);
                _eventLocated += value;
            }
            remove
            {
                _eventLocated -= value;
                EventRemove(_eventLocated);
            }
        }
        event EventHandler<EventEventArgs> _eventDeleted;
        public event EventHandler<EventEventArgs> EventDeleted
        {
            add
            {
                EventAdd(_eventDeleted);
                _eventDeleted += value;
            }
            remove
            {
                _eventDeleted -= value;
                EventRemove(_eventDeleted);
            }
        }
        // do not implement this in remote client as is used only for debugging puproses
#pragma warning disable CS0067
        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        // do not implement this in remote client as is used only for debugging puproses
        public event EventHandler<EventEventArgs> VisibleEventAdded;
        // do not implement this in remote client as is used only for debugging puproses
        public event EventHandler<EventEventArgs> VisibleEventRemoved;
        // do not implement this in remote client as is used only for debugging puproses
        public event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;
#pragma warning restore

        protected override void OnEventNotification(string eventName, EventArgs eventArgs)
        {
            switch (eventName)
            {
                case nameof(IEngine.EngineTick):
                    _engineTick?.Invoke(this, (EngineTickEventArgs)eventArgs);
                    return;
                case nameof(IEngine.EngineOperation):
                    _engineOperation?.Invoke(this, (EngineOperationEventArgs)eventArgs);
                    return;
                case nameof(IEngine.EventLocated):
                    _eventLocated?.Invoke(this, (EventEventArgs)eventArgs);
                    return;
                case nameof(IEngine.EventDeleted):
                    _eventDeleted?.Invoke(this, (EventEventArgs)eventArgs);
                    return;
            }
            base.OnEventNotification(eventName, eventArgs);
        }

        #endregion // Event handling
    }
}
