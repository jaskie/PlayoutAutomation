using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class AnimatedEvent : Event, ITemplated
    {
        public IDictionary<string, string> Fields { get { return Get<Dictionary<string, string>>(); }  set { Set(value); } }

        public TemplateMethod Method { get { return Get<TemplateMethod>(); } set { Set(value); } }

        public int TemplateLayer { get { return Get<int>(); } set { Set(value); } }
    }
}
