using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class EventProxy : IEventProperties
    {
        public double? AudioVolume { get; set; }
        public TimeSpan Duration { get; set; }
        public string EventName { get; set; }
        public TEventType EventType { get; set; }
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
        public bool IsCGEnabled { get; set; }
        public byte Crawl { get; set; }
        public byte Logo { get; set; }
        public byte Parental { get; set; }
        public AutoStartFlags AutoStartFlags { get; set; }
        [DefaultValue(default(TemplateMethod))]
        public TemplateMethod Method { get; set; }
        [DefaultValue(-1)]
        public int TemplateLayer { get; set; }
        public string Command { get; set; }
        public IDictionary<string, string> Fields { get; set; }


        public IEvent InsertAfter(IEvent prior, IEnumerable<IMedia> mediaFiles, IEnumerable<IMedia> animationFiles)
        {
            IEvent newEvent = _toEvent(prior.Engine, mediaFiles, animationFiles);
            prior.InsertAfter(newEvent);
            return newEvent;
        }

        public IEvent InsertUnder(IEvent parent, bool fromEnd, IEnumerable<IMedia> mediaFiles, IEnumerable<IMedia> animationFiles)
        {
            IEvent newEvent = _toEvent(parent.Engine, mediaFiles, animationFiles);
            parent.InsertUnder(newEvent, fromEnd);
            return newEvent;
        }

        public IEvent InsertBefore(IEvent prior, IEnumerable<IMedia> mediaFiles, IEnumerable<IMedia> animationFiles)
        {
            IEvent newEvent = _toEvent(prior.Engine, mediaFiles, animationFiles);
            prior.InsertBefore(newEvent);
            return newEvent;
        }

        public IEvent InsertRoot(IEngine engine, IEnumerable<IMedia> mediaFiles, IEnumerable<IMedia> animationFiles)
        {
            IEvent newEvent = _toEvent(engine, mediaFiles, animationFiles);
            engine.AddRootEvent(newEvent);
            return newEvent;
        }


        private IEvent _toEvent(IEngine engine, IEnumerable<IMedia> mediaFiles, IEnumerable<IMedia> animationFiles)
        {
            IEvent result = null;
            try
            {
                result = engine.CreateNewEvent(
                        videoLayer: Layer,
                        eventType: EventType,
                        startType: TStartType.None,
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
                        //isLoop: IsLoop,
                        isCGEnabled: IsCGEnabled,
                        crawl: Crawl,
                        logo: Logo,
                        parental: Parental,
                        autoStartFlags: AutoStartFlags,
                        command: Command,
                        fields: Fields,
                        method: Method,
                        templateLayer: TemplateLayer
                    );
                // find media if Guid not set
                if ((EventType == TEventType.Movie || EventType == TEventType.StillImage) && mediaFiles != null && Media != null)
                {
                    IMedia media = null;
                    if (!Guid.Empty.Equals(MediaGuid))
                        media = mediaFiles.FirstOrDefault(m => m.MediaGuid.Equals(MediaGuid));
                    if (media == null && !string.IsNullOrEmpty((Media as IPersistentMediaProperties)?.IdAux))
                        media = mediaFiles.FirstOrDefault(m => m is IPersistentMedia && ((IPersistentMedia)m).IdAux == ((IPersistentMediaProperties)Media).IdAux);
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
                if (EventType == TEventType.Animation && animationFiles != null && Media != null)
                {
                    IMedia media = null;
                    if (!Guid.Empty.Equals(MediaGuid))
                        media = animationFiles.FirstOrDefault(m => m.MediaGuid.Equals(MediaGuid));
                    if (media == null
                        && Media is IPersistentMediaProperties
                        && !string.IsNullOrEmpty(((IPersistentMediaProperties)Media).IdAux))
                        media = animationFiles.FirstOrDefault(m => m is IPersistentMedia && ((IPersistentMedia)m).IdAux == ((IPersistentMediaProperties)Media).IdAux);
                    if (media == null)
                        media = animationFiles.FirstOrDefault(m => m.FileName == Media.FileName && m.FileSize == Media.FileSize);
                    result.Media = media;
                }
                // add subevents
                IEvent ne = null;
                foreach (EventProxy seProxy in SubEvents)
                {
                    switch (seProxy.StartType)
                    {
                        case TStartType.WithParent:
                            ne = seProxy._toEvent(engine, mediaFiles, animationFiles);
                            result.InsertUnder(ne, false);
                            break;
                        case TStartType.WithParentFromEnd:
                            ne = seProxy._toEvent(engine, mediaFiles, animationFiles);
                            result.InsertUnder(ne, true);
                            break;
                        case TStartType.After:
                            if (ne != null)
                            {
                                var e = seProxy._toEvent(engine, mediaFiles, animationFiles);
                                ne.InsertAfter(e);
                                ne = e;
                            }
                            else
                                throw new ApplicationException($"Previous item for {seProxy} not found");
                            break;
                        default:
                            throw new ApplicationException($"Invalid start type of {seProxy}");
                    }
                }
            }
            catch
            {
                result?.Delete();
                throw;
            }
            return result;
        }

        public static EventProxy FromEvent(IEvent source)
        {
            IMedia eventMedia = source.Media;
            return new EventProxy
            {
                AudioVolume = source.AudioVolume,
                Duration = source.Duration,
                EventName = source.EventName,
                EventType = source.EventType,
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
                SubEvents = source.AllSubEvents().Select(FromEvent).ToArray(),
                IsCGEnabled = source.IsCGEnabled,
                Crawl = source.Crawl,
                Logo = source.Logo,
                Parental = source.Parental,
                AutoStartFlags = source.AutoStartFlags,
                Command = (source as ICommandScript)?.Command,
                Fields = source is ITemplated templated && templated.Fields != null ? new Dictionary<string, string>(templated.Fields) : null,
                Method = (source as ITemplated)?.Method ?? TemplateMethod.Add,
                TemplateLayer = (source as ITemplated)?.TemplateLayer ?? -1
            };
        }
    }

}
