using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Server
{
    public class AnimatedEvent : Event, ITemplated
    {
        private int _templateLayer;
        private TemplateMethod _method;
        private readonly SimpleDictionary<string, string> _fields;
        
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
            _fields = new SimpleDictionary<string, string>(fields);
            _fields.DictionaryOperation += _fields_DictionaryOperation;
            _method = method;
            _templateLayer = templateLayer;
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            _fields.DictionaryOperation -= _fields_DictionaryOperation;
        }
        
        [JsonProperty]
        public IDictionary<string, string> Fields
        {
            get { return _fields; }
            set
            {
                _fields.Clear();
                foreach (var kvp in value)
                    _fields.Add(kvp);
            }
        }

        [JsonProperty]
        public TemplateMethod Method { get { return _method; } set { SetField(ref _method, value); } }

        [JsonProperty]
        public int TemplateLayer { get { return _templateLayer; } set { SetField(ref _templateLayer, value); } }

        private void _fields_DictionaryOperation(object sender, DictionaryOperationEventArgs<string, string> e)
        {
            IsModified = true;
        }

    }
}
