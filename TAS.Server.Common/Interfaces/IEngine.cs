using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IEngine : IEngineProperties, IPreview, INotifyPropertyChanged
    {
        long FrameTicks { get; }
        IPlayoutServerChannel PlayoutChannelPRI { get; }
        IPlayoutServerChannel PlayoutChannelSEC { get; }
        IMediaManager MediaManager { get; }
        ConnectionStateRedundant DatabaseConnectionState { get; }
        bool Pst2Prv { get; set; }

        decimal ProgramAudioVolume { get; set; }
        bool FieldOrderInverted { get; set; }
        TEngineState EngineState { get; }

        RationalNumber FrameRate { get; }
        IEnumerable<IEventClient> RootEvents { get; }
        void AddRootEvent(IEvent ev);
        List<IEventClient> FixedTimeEvents { get; }

        IEvent AddNewEvent(
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
            );

        void Load(IEventClient aEvent);
        void StartLoaded();
        void Start(IEventClient aEvent);
        void Clear();
        void Clear(VideoLayer aVideoLayer);
        void RestartRundown(IEventClient ARundown);
        IEventClient ForcedNext { get; set; }
        void Schedule(IEventClient aEvent);
        void ReSchedule(IEventClient aEvent);
        void Restart();

        DateTime CurrentTime { get; }
        TimeSpan AlignTimeSpan(TimeSpan ts);
        DateTime AlignDateTime(DateTime dt);
        bool DateTimeEqal(DateTime dt1, DateTime dt2);

        ICGElementsController CGElementsController { get; }

        //MediaDeleteDenyReason CanDeleteMedia(IServerMedia serverMedia);
        void SearchMissingEvents();
        IEventClient Playing { get; }
        IEventClient NextToPlay { get; }
        IEventClient NextWithRequestedStartTime { get; }

        event EventHandler<IEventEventArgs> EventSaved;
        event EventHandler<IEventEventArgs> EventDeleted;
        event EventHandler<EngineTickEventArgs> EngineTick;
        event EventHandler<EngineOperationEventArgs> EngineOperation;
        event EventHandler<CollectionOperationEventArgs<IEventClient>> VisibleEventsOperation;
        event EventHandler<CollectionOperationEventArgs<IEventClient>> RunningEventsOperation;
        event EventHandler<CollectionOperationEventArgs<IEventClient>> FixedTimeEventOperation;
    }

    public interface IEngineProperties : IPersistent
    {
        TAspectRatioControl AspectRatioControl { get; set; }
        string EngineName { get; set; }
        int TimeCorrection { get; set; }
        TVideoFormat VideoFormat { get; set; }
        double VolumeReferenceLoudness { get; set; }
        bool EnableCGElementsForNewEvents { get; set; }
        TCrawlEnableBehavior CrawlEnableBehavior { get; set; }
        int CGStartDelay { get; set; }
        ulong Instance { get; set; }
        ulong IdServerPRI { get; set; }
        int ServerChannelPRI { get; set; }
        ulong IdServerSEC { get; set; }
        int ServerChannelSEC { get; set; }
        ulong IdServerPRV { get; set; }
        int ServerChannelPRV { get; set; }
        ulong IdArchive { get; set; }
    }
}
