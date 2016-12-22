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

namespace TAS.Client.Model
{
    public class Engine : ProxyBase, IEngine
    {
        public Engine()
        {
            Debug.WriteLine("Engine created.");
        }

        public TAspectRatioControl AspectRatioControl { get { return Get<TAspectRatioControl>(); } set { SetField(value); } }

        public DateTime CurrentTime { get { return Get<DateTime>(); } }

        public string EngineName { get { return Get<string>(); } set { SetField(value); } }

        public TEngineState EngineState { get { return Get<TEngineState>(); } set { SetField(value); } }

        public IEvent ForcedNext { get { return Get<IEvent>(); } set { SetField(value); } }

        public VideoFormatDescription FormatDescription { get { return Get<VideoFormatDescription>(); } set { SetField(value); } }

        public RationalNumber FrameRate { get { return Get<RationalNumber>(); } set { SetField(value); } }

        public long FrameTicks { get { return Get<long>(); } set { SetField(value); } }

        [JsonProperty(nameof(IEngine.CGElementsController))]
        private CGElementsController _cGElementsController { get { return Get<CGElementsController>(); } set { SetField(value); } }
        [JsonIgnore]
        public ICGElementsController CGElementsController { get { return _cGElementsController; } }

        public bool EnableCGElementsForNewEvents { get; set; }

        public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }

        public int CGStartDelay { get; set; }

        public ulong Id { get { return Get<ulong>(); } set { SetField(value); } }

        public ulong IdArchive { get { return Get<ulong>(); } set { SetField(value); } }

        public ulong IdServerPRI { get { return Get<ulong>(); } set { SetField(value); } }

        public ulong IdServerPRV { get { return Get<ulong>(); } set { SetField(value); } }

        public ulong IdServerSEC { get { return Get<ulong>(); } set { SetField(value); } }

        public ulong Instance { get { return Get<ulong>(); } set { SetField(value); } }

        public bool FieldOrderInverted { get { return Get<bool>(); } set { Set(value); } }


        public IMediaManager MediaManager { get { return Get<MediaManager>(); } set { SetField(value); } }

        public IEvent NextToPlay
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEvent NextWithRequestedStartTime
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IPlayoutServerChannel PlayoutChannelPRI { get { return Get<IPlayoutServerChannel>(); } set { SetField(value); } }

        public IPlayoutServerChannel PlayoutChannelSEC { get { return Get<IPlayoutServerChannel>(); } set { SetField(value); } }

        #region IPreview
        public IPlayoutServerChannel PlayoutChannelPRV { get { return Get<IPlayoutServerChannel>(); } set { SetField(value); } }
        public decimal PreviewAudioLevel { get { return Get<decimal>(); } set { Set(value); } }
        public bool PreviewIsPlaying { get { return Get<bool>(); } set { SetField(value); } }
        public bool PreviewLoaded { get { return Get<bool>(); } set { SetField(value); } }
        [JsonProperty(nameof(IEngine.PreviewMedia))]
        private Media _previewMedia { get { return Get<Media>(); } set { SetField(value); } }
        [JsonIgnore]
        public IMedia PreviewMedia { get { return Get<Media>(); } }
        public long PreviewPosition { get { return Get<long>(); } set { Set(value); } }
        public long PreviewSeek { get { return Get<long>(); } set { SetField(value); } }
        public void PreviewLoad(IMedia media, long seek, long duration, long position, decimal audioLevel)
        {
            Invoke(parameters: new object[] { media, seek, duration, position, audioLevel });
        }

        public bool PreviewPause()
        {
            return Query<bool>();
        }

        public bool PreviewPlay()
        {
            return Query<bool>();
        }

        public void PreviewUnload()
        {
            Invoke();
        }

        public IMedia FindPreviewMedia(IMedia media)
        {
            return Query<Media>(parameters: new object[] { media });
        }

        #endregion IPreview

        public decimal ProgramAudioVolume
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool Pst2Prv
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public SynchronizedCollection<IEvent> RootEvents
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int ServerChannelPRI { get; set; }
        public int ServerChannelPRV { get; set; }
        public int ServerChannelSEC { get; set; }

        public int TimeCorrection
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public TVideoFormat VideoFormat
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public double VolumeReferenceLoudness
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ConnectionStateRedundant DatabaseConnectionState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEvent Playing
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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
                    IEnumerable<ICommandScriptItem> commands = null,
                    IDictionary<string, string> fields = null,
                    TemplateMethod method = TemplateMethod.Add,
                    int templateLayer = -1
            )
        {
            throw new NotImplementedException();
        }

        public DateTime AlignDateTime(DateTime dt)
        {
            throw new NotImplementedException();
        }

        public TimeSpan AlignTimeSpan(TimeSpan ts)
        {
            throw new NotImplementedException();
        }

        public MediaDeleteDenyReason CanDeleteMedia(IServerMedia serverMedia)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            throw new NotImplementedException();
        }

        public bool DateTimeEqal(DateTime dt1, DateTime dt2)
        {
            throw new NotImplementedException();
        }

        public void Load(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public void RemoveEvent(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public void ReScheduleDelayed(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void RestartRundown(IEvent ARundown)
        {
            throw new NotImplementedException();
        }

        public void Schedule(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public void SearchMissingEvents()
        {
            throw new NotImplementedException();
        }

        public void Start(IEvent aEvent)
        {
            throw new NotImplementedException();
        }

        public void StartLoaded()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<EngineOperationEventArgs> EngineOperation;
        public event EventHandler<EngineTickEventArgs> EngineTick;
        public event EventHandler<IEventEventArgs> EventSaved;
        public event EventHandler<IEventEventArgs> EventDeleted;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> VisibleEventsOperation;
        public event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;
    }
}
