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
    public interface IEngine : IEngineConfig, IPreview, INotifyPropertyChanged
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

        IEvent AddEvent(
                    UInt64 idRundownEvent,
                    VideoLayer videoLayer,
                    TEventType eventType,
                    TStartType startType,
                    TPlayState playState,
                    DateTime scheduledTime,
                    TimeSpan duration,
                    TimeSpan scheduledDelay,
                    TimeSpan scheduledTC,
                    Guid mediaGuid,
                    string eventName,
                    DateTime startTime,
                    TimeSpan startTC,
                    TimeSpan? requestedStartTime,
                    TimeSpan transitionTime,
                    TTransitionType transitionType,
                    decimal? audioVolume,
                    UInt64 idProgramme,
                    string idAux,
                    bool isEnabled,
                    bool isHold,
                    bool isLoop,
                    EventGPI gpi);

        void RemoveEvent(IEvent aEvent);
        IEvent CreateNewEvent();

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

        event EventHandler<IEventEventArgs> EventSaved;
        event EventHandler<EngineTickEventArgs> EngineTick;
        event EventHandler<EngineOperationEventArgs> EngineOperation;
        event EventHandler<DictionaryOperationEventArgs<VideoLayer, IEvent>> VisibleEventsOperation;
        event EventHandler<DictionaryOperationEventArgs<VideoLayer, IEvent>> LoadedNextEventsOperation;
        event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
    }
}
