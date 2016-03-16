using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelMovieViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelMovieViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) { }


    }
}
