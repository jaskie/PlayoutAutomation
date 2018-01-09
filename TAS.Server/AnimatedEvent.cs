using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class AnimatedEvent : Event, ITemplated
    {
        private int _templateLayer;
        private TemplateMethod _method;
        private readonly Dictionary<string, string> _fields;
        
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
                        false, 0, 0, 0
                        )
        {
            _fields = fields == null ? new Dictionary<string, string>() : new Dictionary<string, string>(fields);
            _method = method;
            _templateLayer = templateLayer;
        }

        [JsonProperty]
        public IDictionary<string, string> Fields
        {
            get
            {
                lock (((IDictionary) _fields).SyncRoot)
                    return new Dictionary<string, string>(_fields);
            }
            set
            {
                lock (((IDictionary) _fields).SyncRoot)
                {
                    _fields.Clear();
                    foreach (var kvp in value)
                        _fields.Add(kvp.Key, kvp.Value);
                }
                IsModified = true;
            }
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
