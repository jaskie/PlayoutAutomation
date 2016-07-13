using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class AnimatedEvent : Event, IAnimatedEvent
    {
        public AnimatedEvent(
                    Engine engine,
                    UInt64 idRundownEvent,
                    UInt64 idEventBinding,
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
                    EventGPI gpi) : base(
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
                        gpi,
                        AutoStartFlags.None
                        )
        {
            _fields = new SimpleDictionary<string, string>();
            _fields.DictionaryOperation += _fields_DictionaryOperation;
        }

        private void _fields_DictionaryOperation(object sender, DictionaryOperationEventArgs<string, string> e)
        {
            Modified = true;
        }

        private readonly SimpleDictionary<string, string> _fields;
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

        private TemplateMethod _method;
        public TemplateMethod Method { get { return _method; } set { SetField(ref _method, value, "Method"); } }

        private int _templateLayer;
        public int TemplateLayer { get { return _templateLayer; } set { SetField(ref _templateLayer, value, "TemplateLayer"); } }

    }
}
