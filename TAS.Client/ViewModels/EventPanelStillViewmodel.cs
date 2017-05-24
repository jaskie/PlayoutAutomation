using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelStillViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelStillViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) { }

    }
}
