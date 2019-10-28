using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
                        engine,
                        idRundownEvent,
                        idEventBinding,
                        videoLayer,
                        TEventType.Animation,
                        startType,
                        playState,
                        scheduledTime,
                        duration,
                        scheduledDelay,
                        TimeSpan.Zero,
                        mediaGuid,
                        eventName,
                        startTime,
                        TimeSpan.Zero,
                        null,
                        TimeSpan.Zero,
                        TimeSpan.Zero,
                        TTransitionType.Cut,
                        TEasing.None,
                        0,
                        0,
                        string.Empty,
                        isEnabled,
                        false,
                        false,
                        AutoStartFlags.None,
                        false, 0, 0, 0, -1
                        )
        {
            _fields = fields == null ? new Dictionary<string, string>() : new Dictionary<string, string>(fields);
            _method = method;
            _templateLayer = templateLayer;
        }

        [JsonProperty]
        public Dictionary<string, string> Fields
        {
            get => _fields;
            set => SetField(ref _fields, value == null ? new Dictionary<string, string>() : new Dictionary<string, string>(value));
        }

        [JsonProperty]
        public TemplateMethod Method {
            get => _method;
            set => SetField(ref _method, value);
        }

        [JsonProperty]
        public int TemplateLayer {
            get => _templateLayer;
            set => SetField(ref _templateLayer, value);
        }


    }
}
