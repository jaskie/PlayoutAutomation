using jNet.RPC;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class AnimatedEvent : Event, ITemplated
    {
        private int _templateLayer;
        private TemplateMethod _method;
        private Dictionary<string, string> _fields;
        
        internal AnimatedEvent(
                    Engine engine,
                    ulong idRundownEvent,
                    ulong idEventBinding,
                    VideoLayer videoLayer,
                    TStartType startType,
                    TPlayState playState,
                    DateTime scheduledTime,
                    TimeSpan duration,
                    TimeSpan scheduledDelay,
                    Guid mediaGuid,
                    string eventName,
                    DateTime startTime,
                    bool isEnabled,
                    IDictionary<string, string> fields,
                    TemplateMethod method,
                    int templateLayer
            ) : base(
                        engine: engine,
                        idRundownEvent: idRundownEvent,
                        idEventBinding: idEventBinding,
                        videoLayer: videoLayer,
                        eventType: TEventType.Animation,
                        startType: startType,
                        playState: playState,
                        scheduledTime: scheduledTime,
                        duration: duration,
                        scheduledDelay: scheduledDelay,
                        scheduledTC: TimeSpan.Zero,
                        mediaGuid: mediaGuid,
                        eventName: eventName,
                        startTime: startTime,
                        startTC: TimeSpan.Zero,
                        requestedStartTime: null,
                        transitionTime: TimeSpan.Zero,
                        transitionPauseTime: TimeSpan.Zero,
                        transitionType: TTransitionType.Cut,
                        transitionEasing: TEasing.None,
                        audioVolume: 0,
                        idProgramme: 0,
                        idAux: string.Empty,
                        isEnabled: isEnabled,
                        isHold: false,
                        isLoop: false,
                        autoStartFlags: AutoStartFlags.None,
                        isCGEnabled: false,
                        crawl: 0,
                        logo: 0,
                        parental: 0,
                        routerPort: -1,
                        recordingInfo: null,
                        signalId: 0
                        )
        {
            _fields = fields == null ? new Dictionary<string, string>() : new Dictionary<string, string>(fields);
            _method = method;
            _templateLayer = templateLayer;
        }

        [DtoMember]
        public Dictionary<string, string> Fields
        {
            get => _fields;
            set => SetField(ref _fields, value == null ? new Dictionary<string, string>() : new Dictionary<string, string>(value));
        }

        [DtoMember]
        public TemplateMethod Method {
            get => _method;
            set => SetField(ref _method, value);
        }

        [DtoMember]
        public int TemplateLayer {
            get => _templateLayer;
            set => SetField(ref _templateLayer, value);
        }


    }
}
