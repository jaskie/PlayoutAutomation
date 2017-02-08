using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class Engine : ProxyBase, IEngine
    {
        public Engine()
        {
            Debug.WriteLine("Engine created.");
        }

        public DateTime CurrentTime { get { return Get<DateTime>(); } }

        public string EngineName { get { return Get<string>(); } set { SetLocalValue(value); } }

        public TEngineState EngineState { get { return Get<TEngineState>(); } set { SetLocalValue(value); } }

        public IEvent ForcedNext { get { return Get<Event>(); } set { Set(value); } }

        public VideoFormatDescription FormatDescription { get { return Get<VideoFormatDescription>(); } set { SetLocalValue(value); } }

        public RationalNumber FrameRate { get { return Get<RationalNumber>(); } set { SetLocalValue(value); } }

        public long FrameTicks { get { return Get<long>(); } set { SetLocalValue(value); } }

        [JsonProperty(nameof(IEngine.CGElementsController))]
        private CGElementsController _cGElementsController { get { return Get<CGElementsController>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public ICGElementsController CGElementsController { get { return _cGElementsController; } }

        public bool EnableCGElementsForNewEvents { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public TCrawlEnableBehavior CrawlEnableBehavior { get { return Get<TCrawlEnableBehavior>(); } set { SetLocalValue(value); } }

        public bool FieldOrderInverted { get { return Get<bool>(); } set { Set(value); } }

        public IMediaManager MediaManager { get { return Get<MediaManager>(); } set { SetLocalValue(value); } }

        public IEvent NextToPlay { get { return Get<Event>(); } set { SetLocalValue(value); } }

        public IEvent NextWithRequestedStartTime { get { return Get<Event>(); } set { SetLocalValue(value); } }

        public IPlayoutServerChannel PlayoutChannelPRI { get { return Get<PlayoutServerChannel>(); } set { SetLocalValue(value); } }

        public IPlayoutServerChannel PlayoutChannelSEC { get { return Get<PlayoutServerChannel>(); } set { SetLocalValue(value); } }

        public bool IsWideScreen { get { return Get<bool>(); }  set { Set(value); } }

        #region IPreview
        public IPlayoutServerChannel PlayoutChannelPRV { get { return Get<IPlayoutServerChannel>(); } set { SetLocalValue(value); } }
        public decimal PreviewAudioLevel { get { return Get<decimal>(); } set { Set(value); } }
        public bool PreviewIsPlaying { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public bool PreviewLoaded { get { return Get<bool>(); } set { SetLocalValue(value); } }
        [JsonProperty(nameof(IEngine.PreviewMedia))]
        private Media _previewMedia { get { return Get<Media>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IMedia PreviewMedia { get { return _previewMedia; } }
        public long PreviewPosition { get { return Get<long>(); } set { Set(value); } }
        public long PreviewSeek { get { return Get<long>(); } set { SetLocalValue(value); } }
        public void PreviewLoad(IMedia media, long seek, long duration, long position, decimal audioLevel)
        {
            Invoke(parameters: new object[] { media, seek, duration, position, audioLevel });
        }

        public void PreviewPause() { Invoke(); }

        public void PreviewPlay() { Invoke(); }

        public void PreviewUnload() { Invoke(); }

        #endregion IPreview

        public decimal ProgramAudioVolume { get { return Get<decimal>(); } set { Set(value); } }

        public bool Pst2Prv { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public IEnumerable<IEvent> GetRootEvents() { return Query<List<IEvent>>(); }

        public int ServerChannelPRI { get; set; }
        public int ServerChannelPRV { get; set; }
        public int ServerChannelSEC { get; set; }

        public TVideoFormat VideoFormat { get { return Get<TVideoFormat>(); } set { SetLocalValue(value); } }


        public ConnectionStateRedundant DatabaseConnectionState { get { return Get<ConnectionStateRedundant>(); } set { SetLocalValue(value); } }
        [JsonProperty(nameof(IEngine.Playing))]
        private Event _playing { get { return Get<Event>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEvent Playing { get { return _playing; } }

        public List<IEvent> FixedTimeEvents
        {
            get
            {
                throw new NotImplementedException();
            }
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
            return Query<Event>(parameters: new object[] { idRundownEvent, idEventBinding , videoLayer, eventType, startType, playState, scheduledTime, duration, scheduledDelay, scheduledTC, mediaGuid, eventName,
                    startTime, startTC, requestedStartTime, transitionTime, transitionPauseTime, transitionType, transitionEasing, audioVolume, idProgramme, idAux, isEnabled, isHold, isLoop, isCGEnabled,
                    crawl, logo, parental, autoStartFlags, command, fields, method, templateLayer});
        }

        public void AddRootEvent(IEvent ev)
        {
            Invoke(parameters: new[] { ev });
        }

        public void Clear() { Invoke(); }

        public void Clear(VideoLayer aVideoLayer) { Invoke(parameters: new[] { aVideoLayer }); } 

        public void ClearMixer() { Invoke(); }

        public void Load(IEvent aEvent) { Invoke(parameters: new[] { aEvent }); }

        public void RemoveEvent(IEvent aEvent) { Invoke(parameters: new[] { aEvent }); }

        public void ReSchedule(IEvent aEvent) { Invoke(parameters: new[] { aEvent }); }

        public void Restart() { Invoke(); }

        public void RestartRundown(IEvent aRundown) { Invoke(parameters: new[] { aRundown }); }

        public void Schedule(IEvent aEvent) { Invoke(parameters: new[] { aEvent }); }

        public void SearchMissingEvents()
        {
            throw new NotImplementedException();
        }

        public void Start(IEvent aEvent) { Invoke(parameters: new[] { aEvent }); }
        
        public void StartLoaded() { Invoke(); }

        public void Execute(string command)
        {
            throw new NotImplementedException(); // method used by server plugin only
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
        event EventHandler<IEventEventArgs> _eventSaved;
        public event EventHandler<IEventEventArgs> EventSaved
        {
            add
            {
                EventAdd(_eventSaved);
                _eventSaved += value;
            }
            remove
            {
                _eventSaved -= value;
                EventRemove(_eventSaved);
            }
        }
        event EventHandler<IEventEventArgs> _eventDeleted;
        public event EventHandler<IEventEventArgs> EventDeleted
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
        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        // do not implement this in remote client as is used only for debugging puproses
        public event EventHandler<CollectionOperationEventArgs<IEvent>> VisibleEventsOperation;
        // do not implement this in remote client as is used only for debugging puproses
        public event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;

        protected override void OnEventNotification(WebSocketMessage e)
        {
            switch (e.MemberName)
            {
                case nameof(IEngine.EngineTick):
                    _engineTick?.Invoke(this, ConvertEventArgs<EngineTickEventArgs>(e));
                    break;
                case nameof(IEngine.EngineOperation):
                    _engineOperation?.Invoke(this, ConvertEventArgs<EngineOperationEventArgs>(e));
                    break;
                case nameof(IEngine.EventSaved):
                    _eventSaved?.Invoke(this, ConvertEventArgs<IEventEventArgs>(e));
                    break;
                case nameof(IEngine.EventDeleted):
                    _eventDeleted?.Invoke(this, ConvertEventArgs<IEventEventArgs>(e));
                    break;
            }
        }

#endregion // Event handling

        public override string ToString()
        {
            return EngineName;
        }

    }
}
