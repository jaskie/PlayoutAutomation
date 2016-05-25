using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client
{
    internal class EventProxy : IEventProperties
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
        public TTransitionType TransitionType { get; set; }
        public EventProxy[] SubEvents { get; set; }

        public void InsertAfter(IEvent prior)
        {
            IEvent newEvent = _toEvent(prior.Engine, this);
            prior.InsertAfter(newEvent);
        }

        public void InsertUnder(IEvent parent)
        {
            IEvent newEvent = _toEvent(parent.Engine, this);
            parent.InsertUnder(newEvent);
        }

        private static IEvent _toEvent(IEngine engine, EventProxy proxy)
        {
            return engine.AddNewEvent(
                    videoLayer: proxy.Layer,
                    eventType: proxy.EventType,
                    startType: proxy.StartType,
                    playState: TPlayState.Scheduled,
                    scheduledTime: proxy.ScheduledTime,
                    duration: proxy.Duration,
                    scheduledDelay: proxy.ScheduledDelay,
                    scheduledTC: proxy.ScheduledTc,
                    mediaGuid: proxy.MediaGuid,
                    eventName: proxy.EventName,
                    requestedStartTime: proxy.RequestedStartTime,
                    transitionTime: proxy.TransitionTime,
                    transitionType: proxy.TransitionType,
                    audioVolume: proxy.AudioVolume,
                    idProgramme: proxy.IdProgramme,
                    idAux: proxy.IdAux,
                    isEnabled: proxy.IsEnabled,
                    isHold: proxy.IsHold,
                    isLoop: proxy.IsLoop,
                    gpi: proxy.GPI
                );
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
                TransitionType = source.TransitionType,
                SubEvents = source.AllSubEvents().Select(e => FromEvent(e)).ToArray(),
            };
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", EventName, SubEvents.Length);
        }

        internal class MediaProxy : IMediaProperties
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
            public string FullPath { get; }
            public DateTime LastUpdated { get; set; }
            public TMediaCategory MediaCategory { get; set; }
            public TParental Parental { get; set; }
            public Guid MediaGuid { get; }
            public string MediaName { get; set; }
            public TMediaStatus MediaStatus { get; set; }
            public TMediaType MediaType { get; set; }
            public TimeSpan TcPlay { get; set; }
            public TimeSpan TcStart { get; set; }
            public TVideoFormat VideoFormat { get; set; }
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
                    VideoFormat = media.VideoFormat
                };
            }
        }
    }

    static class IEventExtensions
    {
        public static IEnumerable<IEvent> AllSubEvents(this IEvent e)
        {
            IEnumerable<IEvent> sel = e.SubEvents.ToList();
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
