using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
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

        #pragma warning restore

        public IDictionary<string, string> Fields { get { return _fields; } set { Set(value); } }

        public TemplateMethod Method { get { return _method; } set { Set(value); } }

        public int TemplateLayer { get { return _templateLayer; } set { Set(value); } }

    }
}
