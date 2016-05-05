using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using TAS.Common;
using TAS.Remoting;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IEngine : IDto, IEngineConfig, IPreview, INotifyPropertyChanged
    {
        VideoFormatDescription FormatDescription { get; }
        long FrameTicks { get; }
        IPlayoutServerChannel PlayoutChannelPRI { get; }
        IPlayoutServerChannel PlayoutChannelSEC { get; }
        IMediaManager MediaManager { get; }
        bool Pst2Prv { get; set; }
        IGpi LocalGpi { get; }
        IGpi Gpi { get; }

        decimal ProgramAudioVolume { get; set; }
        TEngineState EngineState { get; }

        RationalNumber FrameRate { get; }
        SynchronizedCollection<IEvent> RootEvents { get; }

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
                    TTransitionType transitionType = TTransitionType.Cut,
                    decimal? audioVolume = null,
                    UInt64 idProgramme = 0,
                    string idAux = "",
                    bool isEnabled = true,
                    bool isHold = false,
                    bool isLoop = false,
                    EventGPI gpi = default(EventGPI));

        void RemoveEvent(IEvent aEvent);

        void Load(IEvent aEvent);
        void StartLoaded();
        void Start(IEvent aEvent);
        void Clear();
        void Clear(VideoLayer aVideoLayer);
        void RestartRundown(IEvent ARundown);
        IEvent ForcedNext { get; set; }
        void Schedule(IEvent aEvent);
        void ReScheduleAsync(IEvent aEvent);
        void Restart();

        DateTime CurrentTime { get; }
        TimeSpan AlignTimeSpan(TimeSpan ts);
        DateTime AlignDateTime(DateTime dt);
        bool DateTimeEqal(DateTime dt1, DateTime dt2);

        #region GPI
        bool GPIConnected { get; }
        bool GPIEnabled { get; set; }
        bool GPIAspectNarrow { get; set; }
        TCrawl GPICrawl { get; set; }
        TLogo GPILogo { get; set; }
        TParental GPIParental { get; set; }
        bool GPIIsMaster { get; }
        #endregion // GPI

        MediaDeleteDenyReason CanDeleteMedia(IServerMedia serverMedia);
        void SearchMissingEvents();
        IEvent PlayingEvent(VideoLayer layer = VideoLayer.Program);
        IEvent NextToPlay { get; }
        IEvent NextWithRequestedStartTime { get; }

        event EventHandler<IEventEventArgs> EventSaved;
        event EventHandler<EngineTickEventArgs> EngineTick;
        event EventHandler<EngineOperationEventArgs> EngineOperation;
        event EventHandler<DictionaryOperationEventArgs<VideoLayer, IEvent>> VisibleEventsOperation;
        event EventHandler<DictionaryOperationEventArgs<VideoLayer, IEvent>> LoadedNextEventsOperation;
        event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
    }
}
