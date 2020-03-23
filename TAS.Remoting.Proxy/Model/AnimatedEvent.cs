using System.Collections.Generic;
using jNet.RPC;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class AnimatedEvent : Event, ITemplated
    {

#pragma warning disable CS0649

        [DtoMember(nameof(ITemplated.Fields))]
        private Dictionary<string, string> _fields;

        [DtoMember(nameof(ITemplated.Method))]
        private TemplateMethod _method;

        [DtoMember(nameof(ITemplated.TemplateLayer))]
        private int _templateLayer;

#pragma warning restore

        public Dictionary<string, string> Fields { get => _fields; set => Set(value); }

        public TemplateMethod Method { get => _method; set => Set(value); }

        public int TemplateLayer { get => _templateLayer; set => Set(value); }
    }
}
