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

        void AddEvent(IEvent ev);
        void RemoveEvent(IEvent aEvent);
        IEvent CreateEvent();

        void Load(IEvent aEvent);
        void StartLoaded();
        void Start(IEvent aEvent);
        void Clear();
        void Clear(VideoLayer aVideoLayer);
        void RestartRundown(IEvent ARundown);
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
