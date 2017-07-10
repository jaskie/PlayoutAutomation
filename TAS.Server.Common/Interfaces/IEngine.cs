using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Server.Common.Interfaces
{
    public interface IEngine : IPreview, IEngineProperties
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
        IEnumerable<IEvent> GetRootEvents();
        void AddRootEvent(IEvent ev);
        List<IEvent> FixedTimeEvents { get; }

        IEvent CreateNewEvent(
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
            decimal? audioVolume = null,
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
        );

        void Load(IEvent aEvent);
        void StartLoaded();
        void Start(IEvent aEvent);
        void Clear();
        void Clear(VideoLayer aVideoLayer);
        void ClearMixer();
        void RestartRundown(IEvent aRundown);
        void Schedule(IEvent aEvent);
        void ReSchedule(IEvent aEvent);
        void Restart();
        void ForceNext(IEvent aEvent);
        void Execute(string command);
        DateTime CurrentTime { get; }
        ICGElementsController CGElementsController { get; }
        void SearchMissingEvents();
        IEvent Playing { get; }
        IEvent NextToPlay { get; }
        IEvent NextWithRequestedStartTime { get; }
        IEvent ForcedNext { get; }
        bool IsWideScreen { get; }
        event EventHandler<EventEventArgs> EventSaved;
        event EventHandler<EventEventArgs> EventDeleted;
        event EventHandler<EngineTickEventArgs> EngineTick;
        event EventHandler<EngineOperationEventArgs> EngineOperation;
        event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        event EventHandler<EventEventArgs> VisibleEventAdded;
        event EventHandler<EventEventArgs> VisibleEventRemoved;
        event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;
    }

    public interface IEngineProperties
    {
        string EngineName { get; }
        TVideoFormat VideoFormat { get; set; }
        bool EnableCGElementsForNewEvents { get; set; }
        TCrawlEnableBehavior CrawlEnableBehavior { get; set; }
    }

    public interface IEnginePersistent : IEngineProperties, IPersistent
    {
        TAspectRatioControl AspectRatioControl { get; set; }
        int TimeCorrection { get; set; }
        double VolumeReferenceLoudness { get; set; }
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
