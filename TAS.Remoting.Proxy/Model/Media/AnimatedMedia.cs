using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Remoting.Model.Media
{
    public class AnimatedMedia : PersistentMedia, IAnimatedMedia
    {

#pragma warning disable CS0649

        [JsonProperty(nameof(ITemplated.Fields))]
        private Dictionary<string, string> _fields;

        [JsonProperty(nameof(ITemplated.Method))]
        private TemplateMethod _method;

        [JsonProperty(nameof(ITemplated.TemplateLayer))]
        private int _templateLayer;

        [JsonProperty(nameof(ITemplated.ScheduledDelay))]
        private TimeSpan _scheduledDelay;

        [JsonProperty(nameof(ITemplated.StartType))]
        private TStartType _startType;

#pragma warning restore

        public Dictionary<string, string> Fields { get => _fields; set => Set(value); }

        public TemplateMethod Method { get => _method; set => Set(value); }

        public int TemplateLayer { get => _templateLayer; set => Set(value); }

        public TimeSpan ScheduledDelay { get => _scheduledDelay; set => Set(value); }

        public TStartType StartType { get => _startType; set => Set(value); }
    }
}
