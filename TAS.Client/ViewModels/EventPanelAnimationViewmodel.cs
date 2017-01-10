using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelAnimationViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelAnimationViewmodel(IEventClient ev, EventPanelViewmodelBase parent): base(ev, parent) { }
        public override bool IsEnabled { get { return _event.IsEnabled; } }
    }
}
