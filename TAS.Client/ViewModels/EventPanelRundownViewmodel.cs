using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelRundownViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelRundownViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) { }
    }
}
