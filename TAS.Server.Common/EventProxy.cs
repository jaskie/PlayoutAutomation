using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class EventProxy : IEventProperties
    {
        public decimal? AudioVolume { get; set; }
        public TimeSpan Duration { get; set; }
        public string EventName { get; set; }
        public TEventType EventType { get; set; }
        public EventGPI GPI { get; set; }
        public string IdAux { get; set; }
        public ulong IdProgramme { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHold { get; set; }
        public bool IsLoop { get; set; }
        public VideoLayer Layer { get; set; }
        public Guid MediaGuid { get; set; }
        public MediaProxy Media { get; set; }
        public TimeSpan? RequestedStartTime { get; set; }
        public TimeSpan ScheduledDelay { get; set; }
        public TimeSpan ScheduledTc { get; set; }
        public DateTime ScheduledTime { get; set; }
        public TimeSpan StartTc { get; set; }
        public DateTime StartTime { get; set; }
        public TStartType StartType { get; set; }
        public TimeSpan TransitionTime { get; set; }
        public TimeSpan TransitionPauseTime { get; set; }
        public TTransitionType TransitionType { get; set; }
        public TEasing TransitionEasing { get; set; }
        public EventProxy[] SubEvents { get; set; }
        public AutoStartFlags AutoStartFlags { get; set; }
        public IEnumerable<ICommandScriptItem> Commands { get; set; }

        public void InsertAfter(IEvent prior, IEnumerable<IMedia> mediaFiles)
        {
            IEvent newEvent = _toEvent(prior.Engine, mediaFiles);
            prior.InsertAfter(newEvent);
        }

        public void InsertUnder(IEvent parent, IEnumerable<IMedia> mediaFiles)
        {
            IEvent newEvent = _toEvent(parent.Engine, mediaFiles);
            parent.InsertUnder(newEvent);
        }

        private IEvent _toEvent(IEngine engine, IEnumerable<IMedia> mediaFiles)
        {
            IEvent result = null;
            try {
                result = engine.AddNewEvent(
                        videoLayer: Layer,
                        eventType: EventType,
                        startType: StartType,
                        playState: TPlayState.Scheduled,
                        scheduledTime: ScheduledTime,
                        duration: Duration,
                        scheduledDelay: ScheduledDelay,
                        scheduledTC: ScheduledTc,
                        eventName: EventName,
                        requestedStartTime: RequestedStartTime,
                        transitionTime: TransitionTime,
                        transitionPauseTime: TransitionPauseTime,
                        transitionType: TransitionType,
                        transitionEasing: TransitionEasing,
                        audioVolume: AudioVolume,
                        idProgramme: IdProgramme,
                        idAux: IdAux,
                        isEnabled: IsEnabled,
                        isHold: IsHold,
                        isLoop: IsLoop,
                        gpi: GPI,
                        autoStartFlags: AutoStartFlags,
                        commands: Commands
                    );
                // find media if Guid not set
                if (mediaFiles != null && Media != null)
                {
                    IMedia media = null;
                    if (!Guid.Empty.Equals(MediaGuid))
                        media = mediaFiles.FirstOrDefault(m => m.MediaGuid.Equals(MediaGuid));
                    if (media == null
                        && !string.IsNullOrEmpty(Media.IdAux))
                        media = mediaFiles.FirstOrDefault(m => m is IPersistentMedia ? ((IPersistentMedia)m).IdAux == Media.IdAux : false);
                    if (media == null)
                        media = mediaFiles.FirstOrDefault(m => 
                               m.MediaName == Media.MediaName 
                            && m.MediaType == Media.MediaType
                            && m.TcStart == Media.TcStart 
                            && m.Duration == Media.Duration);
                    if (media == null)
                        media = mediaFiles.FirstOrDefault(m => m.FileName == Media.FileName && m.FileSize == Media.FileSize);
                    result.Media = media;
                }
                // add subevents
                IEvent ne = null;
                foreach (EventProxy seProxy in SubEvents)
                {
                    switch (seProxy.StartType)
                    {
                        case TStartType.With:
                            ne = seProxy._toEvent(engine, mediaFiles);
                            result.InsertUnder(ne);
                            break;
                        case TStartType.After:
                            if (ne != null)
                            {
                                IEvent e = seProxy._toEvent(engine, mediaFiles);
                                ne.InsertAfter(e);
                                ne = e;
                            }
                            else
                                throw new ApplicationException(string.Format("Previous item for {0} not found", seProxy));
                            break;
                        default:
                            throw new ApplicationException(string.Format("Invalid start type of { 0 }", seProxy));
                    }
                }
            }
            catch 
            {
                if (result != null)
                    result.Delete();
                throw;
            }
            return result;
        }

        public static EventProxy FromEvent(IEvent source)
        {
            IMedia eventMedia = source.Media;
            return new EventProxy()
            {
                AudioVolume = source.AudioVolume,
                Duration = source.Duration,
                EventName = source.EventName,
                EventType = source.EventType,
                GPI = source.GPI,
                IdAux = source.IdAux,
                IdProgramme = source.IdProgramme,
                IsEnabled = source.IsEnabled,
                IsHold = source.IsHold,
                IsLoop = source.IsLoop,
                Layer = source.Layer,
                MediaGuid = source.MediaGuid,
                Media = eventMedia == null ? null : MediaProxy.FromMedia(eventMedia),
                RequestedStartTime = source.RequestedStartTime,
                ScheduledDelay = source.ScheduledDelay,
                ScheduledTc = source.ScheduledTc,
                ScheduledTime = source.ScheduledTime,
                StartTc = source.StartTc,
                StartTime = source.StartTime,
                StartType = source.StartType,
                TransitionTime = source.TransitionTime,
                TransitionPauseTime = source.TransitionPauseTime,
                TransitionType = source.TransitionType,
                TransitionEasing = source.TransitionEasing,
                SubEvents = source.AllSubEvents().Select(e => FromEvent(e)).ToArray(),
                AutoStartFlags = source.AutoStartFlags,
                Commands = (source as ICommandScript)?.Commands
            };
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", EventName, SubEvents.Length);
        }

        public class MediaProxy : IMediaProperties
        {
            public TAudioChannelMapping AudioChannelMapping { get; set; }
            public decimal AudioLevelIntegrated { get; set; }
            public decimal AudioLevelPeak { get; set; }
            public decimal AudioVolume { get; set; }
            public TimeSpan Duration { get; set; }
            public TimeSpan DurationPlay { get; set; }
            public string FileName { get; set; }
            public ulong FileSize { get; set; }
            public string Folder { get; set; }
            public DateTime LastUpdated { get; set; }
            public TMediaCategory MediaCategory { get; set; }
            public TParental Parental { get; set; }
            public string MediaName { get; set; }
            public TMediaStatus MediaStatus { get; set; }
            public TMediaType MediaType { get; set; }
            public TimeSpan TcPlay { get; set; }
            public TimeSpan TcStart { get; set; }
            public TVideoFormat VideoFormat { get; set; }
            public string IdAux { get; set; }
            public bool FieldOrderInverted { get; set; }
            internal static MediaProxy FromMedia(IMedia media)
            {
                return new MediaProxy()
                {
                    AudioChannelMapping = media.AudioChannelMapping,
                    AudioLevelIntegrated = media.AudioLevelIntegrated,
                    AudioLevelPeak = media.AudioLevelPeak,
                    AudioVolume = media.AudioVolume,
                    Duration = media.Duration,
                    DurationPlay = media.DurationPlay,
                    FileName = media.FileName,
                    FileSize = media.FileSize,
                    Folder = media.Folder,
                    LastUpdated = media.LastUpdated,
                    MediaCategory = media.MediaCategory,
                    MediaName = media.MediaName,
                    MediaStatus = media.MediaStatus,
                    MediaType = media.MediaType,
                    Parental = media.Parental,
                    TcPlay = media.TcPlay,
                    TcStart = media.TcStart,
                    VideoFormat = media.VideoFormat,
                    IdAux = media is IPersistentMedia ? ((IPersistentMedia)media).IdAux : string.Empty,
                    FieldOrderInverted = media.FieldOrderInverted,
                };
            }
        }
    }

    static class IEventExtensions
    {
        public static IEnumerable<IEvent> AllSubEvents(this IEvent e)
        {
            IEnumerable<IEvent> sel = e.SubEvents;
            foreach (IEvent selItem in sel)
            {
                yield return selItem;
                IEvent nextItem = selItem;
                while ((nextItem = nextItem.Next)!= null)
                    yield return nextItem;
            }
        }
    }
}
